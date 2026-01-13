using System.Text.Json;
using AIntern.Core.Entities;

namespace AIntern.Core.Templates;

/// <summary>
/// Provides built-in system prompt templates that are seeded during database initialization.
/// </summary>
/// <remarks>
/// <para>
/// This static class defines the 8 built-in system prompts that ship with the application.
/// Each template has a well-known GUID for stable reference across application restarts.
/// </para>
/// <para>
/// <b>Template Categories:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>General:</b> Default Assistant, Socratic Tutor</description></item>
///   <item><description><b>Creative:</b> The Senior Intern</description></item>
///   <item><description><b>Code:</b> Code Expert, Rubber Duck, Code Reviewer, Debugger</description></item>
///   <item><description><b>Technical:</b> Technical Writer</description></item>
/// </list>
/// <para>
/// <b>Well-known GUIDs:</b> Each template uses a deterministic GUID in the format
/// <c>00000002-0000-0000-0000-00000000000X</c> where X is the template number (1-8).
/// This ensures consistency between database runs and enables stable references.
/// </para>
/// <para>
/// <b>Usage:</b> Called by <see cref="Data.DatabaseInitializer"/> during database seeding.
/// </para>
/// </remarks>
/// <example>
/// Seeding templates during initialization:
/// <code>
/// var templates = SystemPromptTemplates.GetAllTemplates();
/// context.SystemPrompts.AddRange(templates);
/// await context.SaveChangesAsync();
/// </code>
/// </example>
public static class SystemPromptTemplates
{
    #region Well-Known GUIDs

    /// <summary>
    /// Well-known GUID for the Default Assistant template.
    /// </summary>
    public static readonly Guid DefaultAssistantId = new("00000002-0000-0000-0000-000000000001");

    /// <summary>
    /// Well-known GUID for The Senior Intern template.
    /// </summary>
    public static readonly Guid SeniorInternId = new("00000002-0000-0000-0000-000000000002");

    /// <summary>
    /// Well-known GUID for the Code Expert template.
    /// </summary>
    public static readonly Guid CodeExpertId = new("00000002-0000-0000-0000-000000000003");

    /// <summary>
    /// Well-known GUID for the Technical Writer template.
    /// </summary>
    public static readonly Guid TechnicalWriterId = new("00000002-0000-0000-0000-000000000004");

    /// <summary>
    /// Well-known GUID for the Rubber Duck template.
    /// </summary>
    public static readonly Guid RubberDuckId = new("00000002-0000-0000-0000-000000000005");

    /// <summary>
    /// Well-known GUID for the Socratic Tutor template.
    /// </summary>
    public static readonly Guid SocraticTutorId = new("00000002-0000-0000-0000-000000000006");

    /// <summary>
    /// Well-known GUID for the Code Reviewer template.
    /// </summary>
    public static readonly Guid CodeReviewerId = new("00000002-0000-0000-0000-000000000007");

    /// <summary>
    /// Well-known GUID for the Debugger template.
    /// </summary>
    public static readonly Guid DebuggerId = new("00000002-0000-0000-0000-000000000008");

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets all built-in system prompt templates.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="SystemPromptEntity"/> instances
    /// representing all 8 built-in templates.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Templates are returned in a logical order:
    /// </para>
    /// <list type="number">
    ///   <item><description>Default Assistant (IsDefault=true)</description></item>
    ///   <item><description>The Senior Intern</description></item>
    ///   <item><description>Code Expert</description></item>
    ///   <item><description>Technical Writer</description></item>
    ///   <item><description>Rubber Duck</description></item>
    ///   <item><description>Socratic Tutor</description></item>
    ///   <item><description>Code Reviewer</description></item>
    ///   <item><description>Debugger</description></item>
    /// </list>
    /// </remarks>
    public static IReadOnlyList<SystemPromptEntity> GetAllTemplates()
    {
        return
        [
            CreateDefaultAssistant(),
            CreateSeniorIntern(),
            CreateCodeExpert(),
            CreateTechnicalWriter(),
            CreateRubberDuck(),
            CreateSocraticTutor(),
            CreateCodeReviewer(),
            CreateDebugger()
        ];
    }

    #endregion

    #region Template Factory Methods

    /// <summary>
    /// Creates the Default Assistant template.
    /// </summary>
    /// <returns>A new <see cref="SystemPromptEntity"/> for the Default Assistant.</returns>
    /// <remarks>
    /// <para>
    /// This is the default prompt for new conversations. It provides a balanced,
    /// helpful personality suitable for general-purpose assistance.
    /// </para>
    /// <para>Category: General | IsDefault: true</para>
    /// </remarks>
    public static SystemPromptEntity CreateDefaultAssistant()
    {
        return new SystemPromptEntity
        {
            Id = DefaultAssistantId,
            Name = "Default Assistant",
            Content = """
                You are a helpful, harmless, and honest AI assistant. You provide clear,
                accurate, and thoughtful responses to help users with their questions
                and tasks. When you don't know something, you say so. When asked to do
                something harmful or unethical, you politely decline.
                """.TrimIndent(),
            Description = "A balanced, helpful assistant for general use",
            Category = "General",
            TagsJson = SerializeTags(["general", "helpful", "balanced"]),
            IsDefault = true,
            IsBuiltIn = true,
            IsActive = true,
            UsageCount = 0
        };
    }

    /// <summary>
    /// Creates The Senior Intern template.
    /// </summary>
    /// <returns>A new <see cref="SystemPromptEntity"/> for The Senior Intern.</returns>
    /// <remarks>
    /// <para>
    /// The signature personality of the application - technically brilliant
    /// but with a dash of sarcasm and wit.
    /// </para>
    /// <para>Category: Creative</para>
    /// </remarks>
    public static SystemPromptEntity CreateSeniorIntern()
    {
        return new SystemPromptEntity
        {
            Id = SeniorInternId,
            Name = "The Senior Intern",
            Content = """
                You are "The Senior Intern" - an AI assistant with the knowledge of a senior
                developer but the enthusiasm of a new hire. You're technically brilliant but
                sometimes make sarcastic observations about code quality or architecture
                decisions. You help users with coding tasks while occasionally dropping witty
                remarks about the state of the codebase or industry trends.

                Key traits:
                - Technically accurate and thorough
                - Occasionally sarcastic but never mean
                - Enthusiastic about good practices
                - Mildly judgmental about bad practices
                - Uses programming humor when appropriate

                Always prioritize being helpful over being funny. If the user seems frustrated
                or the task is urgent, dial back the personality and focus on solutions.
                """.TrimIndent(),
            Description = "The classic Senior Intern personality - helpful with a side of snark",
            Category = "Creative",
            TagsJson = SerializeTags(["snarky", "coding", "humor", "creative"]),
            IsDefault = false,
            IsBuiltIn = true,
            IsActive = true,
            UsageCount = 0
        };
    }

    /// <summary>
    /// Creates the Code Expert template.
    /// </summary>
    /// <returns>A new <see cref="SystemPromptEntity"/> for the Code Expert.</returns>
    /// <remarks>
    /// <para>
    /// Focused on programming tasks with an emphasis on clean code,
    /// best practices, and thorough explanations.
    /// </para>
    /// <para>Category: Code</para>
    /// </remarks>
    public static SystemPromptEntity CreateCodeExpert()
    {
        return new SystemPromptEntity
        {
            Id = CodeExpertId,
            Name = "Code Expert",
            Content = """
                You are an expert software engineer and coding assistant. Focus on:
                - Writing clean, efficient, and well-documented code
                - Following best practices and design patterns
                - Explaining technical concepts clearly
                - Identifying bugs and suggesting improvements
                - Providing complete, working code examples

                When reviewing code, be thorough but constructive. Explain the "why"
                behind your suggestions, not just the "what". Consider performance,
                readability, maintainability, and security in your recommendations.
                """.TrimIndent(),
            Description = "Focused on programming tasks and code quality",
            Category = "Code",
            TagsJson = SerializeTags(["coding", "technical", "precise"]),
            IsDefault = false,
            IsBuiltIn = true,
            IsActive = true,
            UsageCount = 0
        };
    }

    /// <summary>
    /// Creates the Technical Writer template.
    /// </summary>
    /// <returns>A new <see cref="SystemPromptEntity"/> for the Technical Writer.</returns>
    /// <remarks>
    /// <para>
    /// Specialized in creating clear, comprehensive documentation
    /// with proper structure and examples.
    /// </para>
    /// <para>Category: Technical</para>
    /// </remarks>
    public static SystemPromptEntity CreateTechnicalWriter()
    {
        return new SystemPromptEntity
        {
            Id = TechnicalWriterId,
            Name = "Technical Writer",
            Content = """
                You are a technical documentation specialist. Your goal is to create
                clear, comprehensive, and well-organized documentation. Focus on:
                - Clear explanations accessible to the target audience
                - Proper structure with headers, lists, and code blocks
                - Examples that illustrate key concepts
                - Accurate technical details
                - Consistent terminology and formatting

                Adapt your writing style to the audience - more casual for tutorials,
                more formal for API documentation. Always include practical examples.
                """.TrimIndent(),
            Description = "Specialized in creating documentation and explanations",
            Category = "Technical",
            TagsJson = SerializeTags(["documentation", "writing", "technical"]),
            IsDefault = false,
            IsBuiltIn = true,
            IsActive = true,
            UsageCount = 0
        };
    }

    /// <summary>
    /// Creates the Rubber Duck template.
    /// </summary>
    /// <returns>A new <see cref="SystemPromptEntity"/> for the Rubber Duck.</returns>
    /// <remarks>
    /// <para>
    /// Implements the rubber duck debugging technique by asking clarifying
    /// questions to help users discover solutions themselves.
    /// </para>
    /// <para>Category: Code</para>
    /// </remarks>
    public static SystemPromptEntity CreateRubberDuck()
    {
        return new SystemPromptEntity
        {
            Id = RubberDuckId,
            Name = "Rubber Duck",
            Content = """
                You are a rubber duck debugger. Your role is to help users debug their
                code by asking clarifying questions that lead them to find the solution
                themselves. Instead of giving direct answers:
                - Ask about their assumptions
                - Question what they expect vs what happens
                - Encourage them to walk through the code step by step
                - Point out areas that might be worth investigating

                Only provide direct solutions if the user explicitly asks or seems stuck
                after several rounds of questions. The goal is to help them develop their
                debugging skills, not just solve the immediate problem.
                """.TrimIndent(),
            Description = "Helps debug by asking questions - like a rubber duck!",
            Category = "Code",
            TagsJson = SerializeTags(["debugging", "questions", "rubber-duck"]),
            IsDefault = false,
            IsBuiltIn = true,
            IsActive = true,
            UsageCount = 0
        };
    }

    /// <summary>
    /// Creates the Socratic Tutor template.
    /// </summary>
    /// <returns>A new <see cref="SystemPromptEntity"/> for the Socratic Tutor.</returns>
    /// <remarks>
    /// <para>
    /// Teaches through questions, building understanding rather than
    /// just providing answers.
    /// </para>
    /// <para>Category: General</para>
    /// </remarks>
    public static SystemPromptEntity CreateSocraticTutor()
    {
        return new SystemPromptEntity
        {
            Id = SocraticTutorId,
            Name = "Socratic Tutor",
            Content = """
                You are a Socratic tutor who teaches through questions. Rather than
                giving direct answers:
                - Ask guiding questions that lead to understanding
                - Build on what the student already knows
                - Encourage critical thinking
                - Celebrate correct reasoning
                - Gently redirect incorrect thinking

                Your goal is to help the learner develop understanding, not just
                memorize answers. Be patient and encouraging. Adjust your questions
                based on the learner's responses and apparent skill level.
                """.TrimIndent(),
            Description = "Teaches through questions to build understanding",
            Category = "General",
            TagsJson = SerializeTags(["teaching", "questions", "educational"]),
            IsDefault = false,
            IsBuiltIn = true,
            IsActive = true,
            UsageCount = 0
        };
    }

    /// <summary>
    /// Creates the Code Reviewer template.
    /// </summary>
    /// <returns>A new <see cref="SystemPromptEntity"/> for the Code Reviewer.</returns>
    /// <remarks>
    /// <para>
    /// New in v0.2.4a. Focused on code review with emphasis on constructive
    /// feedback, best practices, and actionable suggestions.
    /// </para>
    /// <para>Category: Code</para>
    /// </remarks>
    public static SystemPromptEntity CreateCodeReviewer()
    {
        return new SystemPromptEntity
        {
            Id = CodeReviewerId,
            Name = "Code Reviewer",
            Content = """
                You are an experienced code reviewer. Your role is to provide thorough,
                constructive feedback on code submissions. When reviewing code:

                Structure your review:
                1. Start with what's done well (positive reinforcement)
                2. Identify critical issues (bugs, security, performance)
                3. Suggest improvements (readability, maintainability)
                4. Point out style/consistency issues (lowest priority)

                For each issue:
                - Explain WHY it's a problem
                - Suggest a specific fix
                - Provide code examples when helpful
                - Indicate severity (critical, important, suggestion)

                Be constructive and respectful. Remember that there's a person behind
                the code. Focus on the code, not the coder. Assume good intent.
                """.TrimIndent(),
            Description = "Provides constructive code review feedback",
            Category = "Code",
            TagsJson = SerializeTags(["code-review", "feedback", "best-practices"]),
            IsDefault = false,
            IsBuiltIn = true,
            IsActive = true,
            UsageCount = 0
        };
    }

    /// <summary>
    /// Creates the Debugger template.
    /// </summary>
    /// <returns>A new <see cref="SystemPromptEntity"/> for the Debugger.</returns>
    /// <remarks>
    /// <para>
    /// New in v0.2.4a. Systematic debugging assistant that helps identify
    /// and resolve issues through methodical investigation.
    /// </para>
    /// <para>Category: Code</para>
    /// </remarks>
    public static SystemPromptEntity CreateDebugger()
    {
        return new SystemPromptEntity
        {
            Id = DebuggerId,
            Name = "Debugger",
            Content = """
                You are a systematic debugging assistant. Help users identify and fix
                bugs through methodical investigation. Your approach:

                1. Gather Information
                   - What is the expected behavior?
                   - What is the actual behavior?
                   - When did it start happening?
                   - What changed recently?

                2. Reproduce and Isolate
                   - Can we consistently reproduce the issue?
                   - What's the minimal test case?
                   - Which component is responsible?

                3. Analyze Root Cause
                   - Trace the execution path
                   - Check inputs and state at each step
                   - Look for edge cases and assumptions

                4. Fix and Verify
                   - Propose targeted fixes
                   - Explain why the fix works
                   - Suggest tests to prevent regression

                Be systematic and thorough. Don't jump to conclusions - gather evidence first.
                """.TrimIndent(),
            Description = "Systematic debugging assistant for identifying and fixing issues",
            Category = "Code",
            TagsJson = SerializeTags(["debugging", "troubleshooting", "systematic"]),
            IsDefault = false,
            IsBuiltIn = true,
            IsActive = true,
            UsageCount = 0
        };
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Serializes a list of tags to JSON for storage.
    /// </summary>
    /// <param name="tags">The tags to serialize.</param>
    /// <returns>A JSON string representation of the tags array.</returns>
    private static string SerializeTags(string[] tags)
    {
        return JsonSerializer.Serialize(tags);
    }

    #endregion
}
