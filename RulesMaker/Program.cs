using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RulesEngine;
using RulesEngine.Models;
using System.IO;
using RulesEngine.Extensions;

/// <summary>
/// Represents a program with methods for testing rules.
/// </summary>
public class Program
{
    /// <summary>
    /// Tests rules defined in a rule file.
    /// </summary>
    public static void WithRuleFile()
    {
        // Load rules from file
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "claimrules.json", SearchOption.AllDirectories);
        if (files == null || files.Length == 0)
            throw new Exception("Rules not found.");

        var fileData = File.ReadAllText(files[0]);
        var workflow = JsonConvert.DeserializeObject<List<Workflow>>(fileData);

        // Create rules engine
        var bre = new RulesEngine.RulesEngine(workflow.ToArray(), null);
        var claim = new Claim()
        {
            ClaimNumber = 123,
            ClaimDateReported = DateTime.Now.AddDays(-1),
            ClaimDate = DateTime.Now.AddDays(1),
            EventDate = DateTime.Now.AddDays(-1)
        };

        // Set inputs for rules engine
        var inputs = new Dictionary<string, Nullable<DateTime>>()
        {
            { "ClaimDateReported", claim.ClaimDateReported},
            { "ClaimDate", claim.ClaimDate },
            { "EventDate", claim.EventDate }
        };

        // Execute rules
        List<RuleResultTree> resultList = bre.ExecuteAllRulesAsync("Claim Workflow", inputs).Result;

        // Process results
        bool outcome = resultList.All(r => r.IsSuccess);
        resultList.OnSuccess((eventName) =>
        {
            Console.WriteLine($"Result '{eventName}' is as expected.");
        });
        resultList.OnFail(() =>
        {
            outcome = false;
        });
        Console.WriteLine($"Test outcome: {outcome}.");
    }

    /// <summary>
    /// Tests rules without using a rule file.
    /// </summary>
    public static void WithoutRuleFile()
    {
        // Define rules
        List<Rule> rules = new List<Rule>();
        var rule = new Rule()
        {
            RuleName = "ClaimDateRule",
            Expression = "ClaimDateReported <= DateTime.Today",
            SuccessEvent = "Success",
            ErrorMessage = "Error: ClaimDateReported is greater than today's date",
            RuleExpressionType = RuleExpressionType.LambdaExpression
        };
        rules.Add(rule);

        // Define workflow
        var workflows = new List<Workflow>();
        Workflow exampleWorkflow = new Workflow();
        exampleWorkflow.WorkflowName = "Claim Workflow";
        exampleWorkflow.Rules = rules;
        workflows.Add(exampleWorkflow);

        // Set input data
        var claim = new Claim()
        {
            ClaimNumber = 123,
            ClaimDateReported = DateTime.Now.AddDays(-1)
        };
        var inputs = new Dictionary<string, Nullable<DateTime>>()
        {
            { "ClaimDateReported",null },
        };

        // Create rules engine
        var bre = new RulesEngine.RulesEngine(workflows.ToArray(), null);

        // Execute rules
        List<RuleResultTree> resultList = bre.ExecuteAllRulesAsync("Claim Workflow", inputs).Result;

        // Process results
        bool outcome = resultList.TrueForAll(r => r.IsSuccess);
        resultList.OnSuccess((eventName) =>
        {
            Console.WriteLine($"Result '{eventName}' is as expected.");
            outcome = true;
        });
        resultList.OnFail(() =>
        {
            outcome = false;
        });
        Console.WriteLine($"Test outcome: {outcome}.");
    }

    /// <summary>
    /// Main method to execute the program.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        WithoutRuleFile();
        WithRuleFile();
    }
}

/// <summary>
/// Represents a claim.
/// </summary>
public class Claim
{
    /// <summary>
    /// Gets or sets the claim number.
    /// </summary>
    public int ClaimNumber { get; set; }

    /// <summary>
    /// Gets or sets the date the claim was reported.
    /// </summary>
    public DateTime ClaimDateReported { get; set; }

    /// <summary>
    /// Gets or sets the date of the claim.
    /// </summary>
    public DateTime ClaimDate { get; set; }

    /// <summary>
    /// Gets or sets the event date.
    /// </summary>
    public DateTime EventDate { get; set; }
}
