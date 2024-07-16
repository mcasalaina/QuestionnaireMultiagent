using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using System.Net;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace QuestionnaireMultiagent
{
    class MultiAgent : INotifyPropertyChanged
    {
        string? DEPLOYMENT_NAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_DEPLOYMENT");
        string? ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        string? BING_API_KEY = Environment.GetEnvironmentVariable("BING_API_KEY");

        private string _Context = "Microsoft Azure AI";
        public string Context
        {
            get { return _Context; }
            set
            {
                if (_Context != value)
                {
                    _Context = value;
                    OnPropertyChanged("Context");
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string _Question = "Does your service offer video generative AI?";
        public string Question
        {
            get { return _Question; }
            set
            {
                if (_Question != value)
                {
                    _Question = value;
                    OnPropertyChanged("Question");
                }
            }
        }

        string? QuestionAnswererPrompt;
        string? AnswerCheckerPrompt;
        string? LinkCheckerPrompt;
        string? ManagerPrompt;
        public MultiAgent()
        {
            updatePrompts();
        }

        public async Task askQuestion()
        {
            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton<IFunctionInvocationFilter, SearchFunctionFilter>();

            Kernel kernel = builder.AddAzureOpenAIChatCompletion(
                            deploymentName: DEPLOYMENT_NAME,
                            endpoint: ENDPOINT,
                            apiKey: API_KEY)
                        .Build();

            BingConnector bing = new BingConnector(BING_API_KEY);

            kernel.ImportPluginFromObject(new WebSearchEnginePlugin(bing), "bing");

            ChatCompletionAgent QuestionAnswererAgent =
                new()
                {
                    Instructions = QuestionAnswererPrompt,
                    Name = "QuestionAnswererAgent",
                    Kernel = kernel,
                    ExecutionSettings = new OpenAIPromptExecutionSettings
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    }
                };

            ChatCompletionAgent AnswerCheckerAgent =
                new()
                {
                    Instructions = AnswerCheckerPrompt,
                    Name = "AnswerCheckerAgent",
                    Kernel = kernel,
                    ExecutionSettings = new OpenAIPromptExecutionSettings
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    }
                };

            ChatCompletionAgent LinkCheckerAgent =
                new()
                {
                    Instructions = LinkCheckerPrompt,
                    Name = "LinkCheckerAgent",
                    Kernel = kernel
                };

            ChatCompletionAgent ManagerAgent =
                new()
                {
                    Instructions = ManagerPrompt,
                    Name = "ManagerAgent",
                    Kernel = kernel
                };

            AgentGroupChat chat =
                new(QuestionAnswererAgent, AnswerCheckerAgent, LinkCheckerAgent, ManagerAgent)
                {
                    ExecutionSettings =
                        new()
                        {
                            TerminationStrategy =
                                new ApprovalTerminationStrategy()
                                {
                                    Agents = [ManagerAgent],
                                    MaximumIterations = 25,
                                }
                        }
                };

            string input = Question;

            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
            Console.WriteLine($"# {AuthorRole.User}: '{input}'");

            await foreach (var content in chat.InvokeAsync())
            {
                switch (content.AuthorName)
                {
                    case "QuestionAnswererAgent":
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case "AnswerCheckerAgent":
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case "LinkCheckerAgent":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case "ManagerAgent":
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                }
                Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
            }
        }

        public void updatePrompts()
        {
            QuestionAnswererPrompt = "You are a question answerer for " + Context + "." +
            " You take in questions from a questionnaire and emit the answers from the perspective of " + Context + "," +
            " using documentation from the public web. You also emit links to any websites you find that help answer the questions.";

            AnswerCheckerPrompt = "You are an answer checker for " + Context + "." +
                "Given a question and an answer, you check the answer for accuracy regarding " + Context + "," +
                "using public web sources when necessary. If everything in the answer is true, you verify the answer by responding \"ANSWER CORRECT.\" with no further explanation." +
                "Otherwise, you respond \"ANSWER INCORRECT - \" and add the portion that is incorrect."
            ;

            LinkCheckerPrompt = """
                You are a link checker. Given a question and an answer that contains links, you verify that the links are working,
                using public web sources when necessary. If all links are working, you verify the answer by responding "LINKS CORRECT." with no further explanation.
                Otherwise, for each bad link, you respond "LINK INCORRECT - " and add the link that is incorrect.
            """;

            ManagerPrompt = """
                You are a manager which reviews the question, the answer to the question, and the links.
                If the answer checker replies "ANSWER INCORRECT", or the link checker replies "LINK INCORRECT," you can reply "reject" and ask the question answerer to correct the answer.
                Once the question has been answered properly, you can approve the request by just responding "approve"
            """;
        }
    }
}
