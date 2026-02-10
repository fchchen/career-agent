namespace CareerAgent.Shared.Constants;

public static class SkillTaxonomy
{
    public static readonly Dictionary<string, List<string>> SkillVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        [".NET"] = [".net", "dotnet", ".net core", ".net framework", "asp.net", "asp.net core", ".net 6", ".net 7", ".net 8"],
        ["C#"] = ["c#", "csharp", "c sharp"],
        ["Angular"] = ["angular", "angular 2+", "angular 12", "angular 13", "angular 14", "angular 15", "angular 16", "angular 17", "angular 18", "angular 19", "angular 20", "angular 21"],
        ["TypeScript"] = ["typescript", "ts"],
        ["JavaScript"] = ["javascript", "js", "es6", "ecmascript"],
        ["SQL Server"] = ["sql server", "mssql", "ms sql", "t-sql", "tsql", "sql", "transact-sql"],
        ["Azure"] = ["azure", "microsoft azure", "azure cloud", "azure devops"],
        ["Azure DevOps"] = ["azure devops", "ado", "tfs", "vsts"],
        ["REST API"] = ["rest", "restful", "rest api", "web api", "minimal api"],
        ["Entity Framework"] = ["entity framework", "ef core", "ef", "entity framework core"],
        ["Git"] = ["git", "github", "gitlab", "bitbucket"],
        ["Docker"] = ["docker", "containers", "containerization"],
        ["Kubernetes"] = ["kubernetes", "k8s", "aks"],
        ["CI/CD"] = ["ci/cd", "ci cd", "continuous integration", "continuous delivery", "continuous deployment", "pipelines"],
        ["Agile"] = ["agile", "scrum", "kanban", "sprint"],
        ["React"] = ["react", "reactjs", "react.js"],
        ["Node.js"] = ["node", "node.js", "nodejs"],
        ["Python"] = ["python"],
        ["AWS"] = ["aws", "amazon web services"],
        ["Microservices"] = ["microservices", "micro-services", "microservice architecture"],
        ["RabbitMQ"] = ["rabbitmq", "rabbit mq"],
        ["Redis"] = ["redis"],
        ["SignalR"] = ["signalr"],
        ["Blazor"] = ["blazor", "blazor server", "blazor wasm"],
        ["LINQ"] = ["linq"],
        ["HTML/CSS"] = ["html", "css", "html5", "css3", "sass", "scss"],
    };

    public static readonly List<string> CoreSkills =
    [
        ".NET", "C#", "Angular", "TypeScript", "SQL Server", "Azure"
    ];

    public static readonly List<string> StrongSkills =
    [
        "REST API", "Entity Framework", "Git", "CI/CD", "Agile",
        "JavaScript", "HTML/CSS", "Docker", "Azure DevOps"
    ];

    public static readonly List<string> BonusSkills =
    [
        "Microservices", "RabbitMQ", "Redis", "SignalR", "Blazor",
        "Kubernetes", "React", "Node.js", "Python", "AWS", "LINQ"
    ];

    public static string? NormalizeSkill(string rawSkill)
    {
        var trimmed = rawSkill.Trim();
        foreach (var (canonical, variants) in SkillVariants)
        {
            if (canonical.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                return canonical;
            if (variants.Any(v => v.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
                return canonical;
        }
        return null;
    }

    public static List<string> ExtractSkills(string text)
    {
        var found = new HashSet<string>();
        var lowerText = text.ToLowerInvariant();

        foreach (var (canonical, variants) in SkillVariants)
        {
            foreach (var variant in variants)
            {
                if (lowerText.Contains(variant, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(canonical);
                    break;
                }
            }
        }

        return found.ToList();
    }

    public static double GetSkillWeight(string skill)
    {
        if (CoreSkills.Contains(skill)) return 1.0;
        if (StrongSkills.Contains(skill)) return 0.6;
        if (BonusSkills.Contains(skill)) return 0.3;
        return 0.1;
    }
}
