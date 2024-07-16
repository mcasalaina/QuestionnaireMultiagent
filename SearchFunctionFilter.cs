using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace QuestionnaireMultiagent
{
    class SearchFunctionFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            // We'll restore the color after we're done
            var prevColor = Console.ForegroundColor;

            // Indicate that the assistant is calling a function, but only if it's not an internal function
            var isInternal = context.Function.Name.StartsWith("internal_");
            if (!isInternal)
            {
                var args = context.Arguments.Select(x => $"\"{x.Key}\": \"{x.Value}\"") ?? new List<string>();
                var json = "{" + string.Join(",", args) + "}";

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"\rassistant-function: {context.Function.Name}({json}) = ");
            }

            // Call the next middleware in the pipeline
            await next(context);

            // Indicate that the assistant has finished calling the function, but only if it's not an internal function
            if (!isInternal)
            {
                try
                {
                    var result = context.Result.GetValue<string>() ?? string.Empty;
                    Console.WriteLine(result);

                    Console.ForegroundColor = prevColor;
                    Console.Write("\nAssistant: ");
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
