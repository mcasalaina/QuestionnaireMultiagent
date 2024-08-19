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

        [Test]
        public void TestSaveExcelFile()
        {
            string filename = "..\\..\\..\\..\\SampleQuestionnaire.xlsx";

            //Get an absolute filename
            filename = System.IO.Path.GetFullPath(filename);

            //Copy the file to the present working directory
            System.IO.File.Copy(filename, System.IO.Path.GetFileName(filename), true);

            //Get the full path of the copied file
            filename = System.IO.Path.GetFullPath(System.IO.Path.GetFileName(filename));

            //Assert that the file exists
            Assert.That(System.IO.File.Exists(filename), Is.True);

            MultiAgent multiAgent = new(null);

            // Act
            // Load the local file in this directory called "Sample Questionnaire.xlsx"
            string[,] data = multiAgent.LoadExcelFile(filename);

            // Assert that there is data in at least 1 cell of the array
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Length, Is.GreaterThan(0));

            //Change all the cells in the second column to "Skibidi"
            for (int i = 0; i < data.GetLength(0); i++)
            {
                data[i, 1] = "Skibidi";
            }

            //Save the data back to the file
            multiAgent.SaveExcelFile(filename, data);

            //Load it back again
            string[,] data2 = multiAgent.LoadExcelFile(filename);

            //Check that the data is the same
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    Assert.That(data[i, j], Is.EqualTo(data2[i, j]));
                }
            }

            //Delete the copied file
            System.IO.File.Delete(filename);
        }
    }
}
