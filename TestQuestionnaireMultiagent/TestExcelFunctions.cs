using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuestionnaireMultiagent;

namespace TestQuestionnaireMultiagent
{
    internal class TestExcelFunctions
    {
        [Test]
        public void TestLoadExcelFile()
        {
            string filename = "..\\..\\..\\..\\SampleQuestionnaire2.xlsx";

            //Get an absolute filename
            filename = System.IO.Path.GetFullPath(filename);
            Console.WriteLine("Loading file: " + filename);

            //Assert that the file exists
            Assert.That(System.IO.File.Exists(filename), Is.True);

            MultiAgent multiAgent = new(null);

            // Act
            // Load the local file in this directory called "Sample Questionnaire.xlsx"
            string[,] data = multiAgent.LoadExcelFile(filename);

            // Assert that there is data in at least 1 cell of the array
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Length, Is.GreaterThan(0));

            // Print out the contents of the array to the console
            Console.WriteLine("Data in the array:");
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    Console.Write(data[i, j] + " ");
                }
                Console.WriteLine();
            }
        }
    }
}
