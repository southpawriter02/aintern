namespace AIntern.Services.Tests.Helpers;

using AIntern.Services.Helpers;
using Xunit;

/// <summary>
/// Unit tests for TypeNameExtractor (v0.4.1e).
/// </summary>
public class TypeNameExtractorTests
{
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ C# TYPE EXTRACTION                                                       │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("public class User { }", "User")]
    [InlineData("internal sealed class DataService { }", "DataService")]
    [InlineData("public abstract class BaseEntity { }", "BaseEntity")]
    [InlineData("public partial class PartialClass { }", "PartialClass")]
    [InlineData("public static class Extensions { }", "Extensions")]
    [InlineData("public record UserDto(string Name);", "UserDto")]
    [InlineData("public struct Point { }", "Point")]
    public void ExtractPrimaryTypeName_CSharp_Classes(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "csharp");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("public interface IUserService { }", "IUserService")]
    [InlineData("internal interface IRepository { }", "IRepository")]
    public void ExtractPrimaryTypeName_CSharp_Interfaces(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "csharp");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("public enum Status { Active, Inactive }", "Status")]
    [InlineData("internal enum ErrorCode { None = 0 }", "ErrorCode")]
    public void ExtractPrimaryTypeName_CSharp_Enums(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "csharp");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractPrimaryTypeName_CSharp_ReturnsFirstMatch()
    {
        var content = @"
            public interface IService { }
            public class Service : IService { }
        ";
        // Class pattern matches before interface in priority order
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "csharp");
        Assert.Equal("Service", result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ TYPESCRIPT/JAVASCRIPT TYPE EXTRACTION                                    │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("export class UserService { }", "UserService")]
    [InlineData("export default class ApiClient { }", "ApiClient")]
    [InlineData("class InternalHelper { }", "InternalHelper")]
    [InlineData("export abstract class BaseComponent { }", "BaseComponent")]
    public void ExtractPrimaryTypeName_TypeScript_Classes(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "typescript");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("export interface IConfig { }", "IConfig")]
    [InlineData("interface LocalState { }", "LocalState")]
    public void ExtractPrimaryTypeName_TypeScript_Interfaces(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "typescript");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("export function processData() { }", "processData")]
    [InlineData("export async function fetchUser() { }", "fetchUser")]
    [InlineData("function helperFn() { }", "helperFn")]
    public void ExtractPrimaryTypeName_TypeScript_Functions(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "typescript");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("export const API_URL = 'http://example.com';", "API_URL")]
    [InlineData("const config = { port: 3000 };", "config")]
    public void ExtractPrimaryTypeName_TypeScript_Constants(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "typescript");
        Assert.Equal(expected, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ PYTHON TYPE EXTRACTION                                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("class DataLoader:\n    pass", "DataLoader")]
    [InlineData("class UserService(BaseService):\n    pass", "UserService")]
    public void ExtractPrimaryTypeName_Python_Classes(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "python");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("def process_data():\n    pass", "process_data")]
    [InlineData("async def fetch_user():\n    pass", "fetch_user")]
    public void ExtractPrimaryTypeName_Python_Functions(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "python");
        Assert.Equal(expected, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ GO TYPE EXTRACTION                                                       │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("type UserStore struct {\n}", "UserStore")]
    [InlineData("type Config struct {\n    Port int\n}", "Config")]
    public void ExtractPrimaryTypeName_Go_Structs(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "go");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("type Repository interface {\n}", "Repository")]
    public void ExtractPrimaryTypeName_Go_Interfaces(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "go");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("func main() {\n}", "main")]
    [InlineData("func ProcessData(data []byte) error {\n}", "ProcessData")]
    public void ExtractPrimaryTypeName_Go_Functions(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "go");
        Assert.Equal(expected, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ RUST TYPE EXTRACTION                                                     │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("pub struct Config {\n}", "Config")]
    [InlineData("struct InternalData {}", "InternalData")]
    public void ExtractPrimaryTypeName_Rust_Structs(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "rust");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("pub enum Status { Active, Inactive }", "Status")]
    [InlineData("enum ErrorKind { IoError, ParseError }", "ErrorKind")]
    public void ExtractPrimaryTypeName_Rust_Enums(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "rust");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("pub trait Repository {\n}", "Repository")]
    [InlineData("trait InternalBehavior {}", "InternalBehavior")]
    public void ExtractPrimaryTypeName_Rust_Traits(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "rust");
        Assert.Equal(expected, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ JAVA TYPE EXTRACTION                                                     │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("public class UserService { }", "UserService")]
    [InlineData("public final class Constants { }", "Constants")]
    [InlineData("public abstract class BaseEntity { }", "BaseEntity")]
    public void ExtractPrimaryTypeName_Java_Classes(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "java");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("public interface IRepository { }", "IRepository")]
    [InlineData("interface PackageInterface { }", "PackageInterface")]
    public void ExtractPrimaryTypeName_Java_Interfaces(string content, string expected)
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName(content, "java");
        Assert.Equal(expected, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ NAMESPACE EXTRACTION                                                     │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("namespace AIntern.Core.Models;", "csharp", "AIntern.Core.Models")]
    [InlineData("namespace MyApp { class X {} }", "csharp", "MyApp")]
    [InlineData("package com.example.models;", "java", "com.example.models")]
    [InlineData("package main", "go", "main")]
    public void ExtractNamespace_ExtractsCorrectly(
        string content, string language, string expected)
    {
        var result = TypeNameExtractor.ExtractNamespace(content, language);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractNamespace_ReturnsNull_ForUnsupportedLanguage()
    {
        var result = TypeNameExtractor.ExtractNamespace("class Test {}", "typescript");
        Assert.Null(result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ BUILD SUGGESTED PATH                                                     │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void BuildSuggestedPath_WithNamespace_ConvertsToPath()
    {
        var result = TypeNameExtractor.BuildSuggestedPath(
            "UserService", "AIntern.Core.Services", ".cs");

        Assert.Equal("AIntern/Core/Services/UserService.cs", result);
    }

    [Fact]
    public void BuildSuggestedPath_WithoutNamespace_ReturnsSimplePath()
    {
        var result = TypeNameExtractor.BuildSuggestedPath(
            "UserService", null, ".cs");

        Assert.Equal("UserService.cs", result);
    }

    [Fact]
    public void BuildSuggestedPath_EmptyNamespace_ReturnsSimplePath()
    {
        var result = TypeNameExtractor.BuildSuggestedPath(
            "UserService", "", ".cs");

        Assert.Equal("UserService.cs", result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EXTRACT ALL TYPE NAMES                                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ExtractAllTypeNames_ReturnsAllTypes()
    {
        var content = @"
            public class User { }
            public class Order { }
            public interface IRepository { }
            public enum Status { }
        ";

        var results = TypeNameExtractor.ExtractAllTypeNames(content, "csharp");

        Assert.Equal(4, results.Count);
        Assert.Contains("User", results);
        Assert.Contains("Order", results);
        Assert.Contains("IRepository", results);
        Assert.Contains("Status", results);
    }

    [Fact]
    public void ExtractAllTypeNames_ReturnsEmpty_ForUnknownLanguage()
    {
        var results = TypeNameExtractor.ExtractAllTypeNames("class X {}", "unknown");
        Assert.Empty(results);
    }

    [Fact]
    public void ExtractAllTypeNames_ReturnsEmpty_ForEmptyContent()
    {
        var results = TypeNameExtractor.ExtractAllTypeNames("", "csharp");
        Assert.Empty(results);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EDGE CASES                                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ExtractPrimaryTypeName_ReturnsNull_ForEmptyContent()
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName("", "csharp");
        Assert.Null(result);
    }

    [Fact]
    public void ExtractPrimaryTypeName_ReturnsNull_ForNullLanguage()
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName("public class X {}", null);
        Assert.Null(result);
    }

    [Fact]
    public void ExtractPrimaryTypeName_ReturnsNull_ForWhitespaceContent()
    {
        var result = TypeNameExtractor.ExtractPrimaryTypeName("   \n\t  ", "csharp");
        Assert.Null(result);
    }
}
