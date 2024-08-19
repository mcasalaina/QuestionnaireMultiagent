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

using System.IO;
using System.Windows.Media;
using System.Windows.Documents;
using Microsoft.Extensions.DependencyInjection;
using ClosedXML.Excel;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604, CS8602

namespace QuestionnaireMultiagent
{
    public class MultiAgent : INotifyPropertyChanged
    {
        MainWindow? mainWindow;

        string? DEPLOYMENT_NAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_DEPLOYMENT");
        string? ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        string? BING_API_KEY = Environment.GetEnvironmentVariable("BING_API_KEY");

        private int _CharacterLimit = 2000;
        public int CharacterLimit
        {
            get { return _CharacterLimit; }
            set
            {
                if (_CharacterLimit != value)
                {
                    _CharacterLimit = value;
                    OnPropertyChanged("CharacterLimit");
                }
            }
        }

        private string _Context = "Microsoft Azure AI";
        public string Context
        {
            get { return _Context; }
            set
            {
                if (_Context != value)
                {
                    _Context = value;
                    UpdatePrompts();
                    OnPropertyChanged("Context");
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _Question = "Does your service offer video generative AI?";
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

        string FinalAnswer = "";

        string? QuestionAnswererPrompt;
        string? AnswerCheckerPrompt;
        string? LinkCheckerPrompt;
        string? ManagerPrompt;
        public MultiAgent(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            UpdatePrompts();
        }

        public async Task AskQuestion()
        {
            //AgentResponse = "Agents running...\n";
            //Remove all the text in mainWindow.ResponseBox
            mainWindow?.ResponseBox.Document.Blocks.Clear();

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

            UpdateResponseBox("Question", input);

            FinalAnswer = "";

            try { 
                await foreach (var content in chat.InvokeAsync())
                {
                    Color color;
                    switch (content.AuthorName)
                    {
                        case "QuestionAnswererAgent":
                            color = Colors.Black;
                            //We assume here that the last time the QuestionAnswererAgent is called, it will have the final answer
                            FinalAnswer = content.Content;
                            break;
                        case "AnswerCheckerAgent":
                            color = Colors.Blue;
                            break;
                        case "LinkCheckerAgent":
                            color = Colors.DarkGoldenrod;
                            break;
                        case "ManagerAgent":
                            color = Colors.DarkGreen;
                            break;
                    }

                    UpdateResponseBox(content.AuthorName, content.Content, color);
                }
            } catch (HttpOperationException e)
            {
                UpdateResponseBox("Agents Terminated Due To Error: ", e.Message, Colors.Red);
            }
        }

        public async Task AnswerInExcelFile(string filename)
        {
            string[,] data = LoadExcelFile(filename);

            //Assume the first row is the header row, the first column is the question column, and the second column is the answer column
            for (int i = 1; i < data.GetLength(0); i++)
            {
                string question = data[i, 0];
                Question = question;
                await AskQuestion();
                data[i, 1] = FinalAnswer;

                SaveExcelFile(filename, data);
            }
        }

        public void SaveExcelFile(string filename, string[,] data)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sheet1");

            int rowCount = data.GetLength(0);
            int colCount = data.GetLength(1);

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    worksheet.Cell(i + 1, j + 1).Value = data[i, j];
                }
            }

            workbook.SaveAs(filename);
        }

        // Loads an Excel file and read the contents of the first sheet into a 2D array
        public string[,] LoadExcelFile(string filename)
        {
            var workbook = new XLWorkbook(filename);
            var worksheet = workbook.Worksheet(1); // Get the first worksheet

            int rowCount = worksheet.LastRowUsed().RowNumber();
            int colCount = worksheet.LastColumnUsed().ColumnNumber();

            string[,] data = new string[rowCount, colCount];

            for (int i = 1; i <= rowCount; i++)
            {
                for (int j = 1; j <= colCount; j++)
                {
                    data[i - 1, j - 1] = worksheet.Cell(i, j).GetString();
                }
            }

            return data;
        }

        public void UpdatePrompts()
        {
            QuestionAnswererPrompt = $"""
                You are a question answerer for {Context}.
                You take in questions from a questionnaire and emit the answers from the perspective of {Context},
                using documentation from the public web. You also emit links to any websites you find that help answer the questions.
                Do not address the user as 'you' - make all responses solely in the third person.
                If you do not find information on a topic, you simply respond that there is no information available on that topic.
                You will emit an answer that is no greater than {CharacterLimit} characters in length.
            """;

            AnswerCheckerPrompt = $"""
                You are an answer checker for {Context}. Your responses always start with either the words ANSWER CORRECT or ANSWER INCORRECT.
                Given a question and an answer, you check the answer for accuracy regarding {Context},
                using public web sources when necessary. If everything in the answer is true, you verify the answer by responding "ANSWER CORRECT." with no further explanation.
                You also ensure that the answer is no greater than {CharacterLimit} characters in length.
                Otherwise, you respond "ANSWER INCORRECT - " and add the portion that is incorrect.
                You do not output anything other than "ANSWER CORRECT" or "ANSWER INCORRECT - <portion>".
            """;

            LinkCheckerPrompt = """
                You are a link checker. Your responses always start with either the words LINKS CORRECT or LINK INCORRECT.
                Given a question and an answer that contains links, you verify that the links are working,
                using public web sources when necessary. If all links are working, you verify the answer by responding "LINKS CORRECT" with no further explanation.
                Otherwise, for each bad link, you respond "LINK INCORRECT - " and add the link that is incorrect.
                You do not output anything other than "LINKS CORRECT" or "LINK INCORRECT - <link>".
            """;

            ManagerPrompt = """
                You are a manager which reviews the question, the answer to the question, and the links.
                If the answer checker replies "ANSWER INCORRECT", or the link checker replies "LINK INCORRECT," you can reply "reject" and ask the question answerer to correct the answer.
                Once the question has been answered properly, you can approve the request by just responding "approve".
                You do not output anything other than "reject" or "approve".
            """;
        }

        public void UpdateResponseBox(string sender, string response)
        {
            UpdateResponseBox(sender, response, Colors.Black);
        }

        public void UpdateResponseBox(string sender, string response, Color color)
        {
            //Update mainWindow.ResponseBox to add the sender in bold, a colon, a space, and the response in normal text
            Paragraph paragraph = new Paragraph();
            Bold bold = new Bold(new Run(sender + ": "));
            
            bold.Foreground = new SolidColorBrush(color);
            
            paragraph.Inlines.Add(bold);
            Run run = new Run(response);
            paragraph.Inlines.Add(run);
            mainWindow?.ResponseBox.Document.Blocks.Add(paragraph);

            Console.WriteLine(sender + ": " + response);
        }
    }
}
