﻿//MIT License

//Copyright (c) 2022 Greg Dennis

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using PBIXInspectorLibrary;
using PBIXInspectorLibrary.Output;
using System.Text.Json;

namespace PBIXInspectorTests;

/// <summary>
/// The code in this SuiteRunner is adapted from Greg Dennis's Json Everything test suite (see https://github.com/gregsdennis/json-everything) to ensure that we're not breaking core JsonLogic functionality.
/// </summary>
public class SuiteRunner
{
    #region PbixTestSuite
    public static IEnumerable<TestCaseData> PbixTestSuite()
    {
        string PBIXFilePath = @"Files\Inventory test.pbix";
        string RulesFilePath = @"Files\Inventory rules test.json";

        Console.WriteLine("Running test suite...");
        return Suite(PBIXFilePath, RulesFilePath);
    }

    [TestCaseSource(nameof(PbixTestSuite))]
    public void RunPbixTest(TestResult testResult)
    {
        Assert.True(testResult.Pass, testResult.Message);
    }
    #endregion

    #region PbipTestSuite
    public static IEnumerable<TestCaseData> PbipTestSuite()
    {
        string PBIPFilePath = @"Files\pbip\Inventory test.pbip";
        string RulesFilePath = @"Files\Inventory rules test.json";

        Console.WriteLine("Running test suite...");
        return Suite(PBIPFilePath, RulesFilePath);
    }

    [TestCaseSource(nameof(PbipTestSuite))]
    public void RunPbipTest(TestResult testResult)
    {
        Assert.True(testResult.Pass, testResult.Message);
    }
    #endregion

    #region JsonLogicSuite 
    /// <summary>
    /// Test PBIX using the base JsonLogicTest file to make sure we didn't break JsonLogic
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<TestCaseData> JsonLogicSuite()
    {
        string PBIXFilePath = @"Files\Inventory test.pbix";
        var testsPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, @"Files\JsonLogicTests.json");

        return Task.Run(async () =>
        {
            string content = null!;
            try
            {
                using var client = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://jsonlogic.com/tests.json");
                using var response = await client.SendAsync(request);

                content = await response.Content.ReadAsStringAsync();

                await File.WriteAllTextAsync(testsPath, content);
            }
            catch (Exception e)
            {
                content ??= await File.ReadAllTextAsync(testsPath);

                Console.WriteLine(e);
            }

            var testSuite = JsonSerializer.Deserialize<JsonLogicTestSuite>(content);
            var inspectionRules = new InspectionRules();
            var rules = testSuite!.Tests.Select(t => new PBIXInspectorLibrary.Rule() { Name = t.Logic, Path = "$", PathErrorWhenNoMatch = false, Test = new PBIXInspectorLibrary.Test() { Logic = t.Logic!, Data = t.Data!, Expected = t.Expected! } });
            inspectionRules.PbiEntries = new List<PbiEntry> { new PbiEntry() { Name = "stub", PbixEntryPath = "Report/Layout", ContentType = "json", CodePage = 1200, Rules = rules } };

            return Suite(PBIXFilePath, inspectionRules);
        }).Result;
    }

    [TestCaseSource(nameof(JsonLogicSuite))]
    public void RunJsonLogicTest(TestResult testResult)
    {
        Assert.True(testResult.Pass, testResult.Message);
    }
    #endregion

    #region SampleSuite 
    public static IEnumerable<TestCaseData> SampleSuite()
    {
        string PBIXFilePath = @"Files\Inventory sample.pbix";
        string RulesFilePath = @"Files\Inventory rules sample.json";

        Console.WriteLine("Running sample suite...");
        return Suite(PBIXFilePath, RulesFilePath);
    }
    

    [TestCaseSource(nameof(SampleSuite))]
    public void RunSample(TestResult testResult)
    {
        Assert.True(testResult.Pass, testResult.Message);
    }
    #endregion

    #region BaseSuite 
    public static IEnumerable<TestCaseData> BaseSuite()
    {
        string PBIXFilePath = @"Files\Inventory sample.pbix";
        string RulesFilePath = @"Files\Base-rules.json";

        Console.WriteLine("Running base suite...");
        return Suite(PBIXFilePath, RulesFilePath);
    }

    [TestCaseSource(nameof(BaseSuite))]
    public void RunBase(TestResult testResult)
    {
        Assert.True(testResult.Pass, testResult.Message);
    }
    #endregion

    #region BaseFailSuite 
    //public static IEnumerable<TestCaseData> BaseFailSuite()
    //{
    //    string PBIXFilePath = @"Files\Inventory sample - fails.pbix";
    //    string RulesFilePath = @"Files\Base-rules.json";

    //    Console.WriteLine("Running base fail suite...");
    //    return Suite(PBIXFilePath, RulesFilePath);
    //}


    //[TestCaseSource(nameof(BaseFailSuite))]
    //public void RunFailSample(TestResult testResult)
    //{
    //    Assert.False(testResult.Pass, testResult.Message);
    //    //Assert.True(testResult.Actual)
    //}
    #endregion

    public static IEnumerable<TestCaseData> Suite(string PBIXFilePath, string RulesFilePath)
    {
        try
        {
            Inspector insp = new Inspector(PBIXFilePath, RulesFilePath);

            var testResults = insp.Inspect();

            return testResults.Select(t => new TestCaseData(t) { TestName = t.RuleName });
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (PBIXInspectorException e)
        {
            Console.WriteLine(e.Message);
        }

        return null;
    }

    public static IEnumerable<TestCaseData> Suite(string PBIXFilePath, InspectionRules inspectionRules)
    {
        try
        {
            Inspector insp = new Inspector(PBIXFilePath, inspectionRules);

            var testResults = insp.Inspect();

            return testResults.Select(t => new TestCaseData(t) { TestName = t.RuleName });
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
        }
        catch (PBIXInspectorException e)
        {
            Console.WriteLine(e.Message);
        }

        return null;
    }
}