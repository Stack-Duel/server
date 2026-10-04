using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Infrastructure.Persistence.Seeders.Problems;

internal sealed class AdditionalProblemsSeeder(
    StackDuelDbContext context,
    Judge0PipelineSeeder pipelineSeeder,
    ITestCaseGenerationJobRepository testCaseGenerationJobRepository
) : IStaticSeeder
{
    private const string FactorialFunctionName = "factorial";

    private static readonly ProblemSeedDefinition[] ProblemSeeds =
    [
        new(
            Slug: "sum-two-integers",
            Title: "Sum Two Integers",
            Question: "Given two integers a and b, return the sum a + b.\n\nExample 1:\nInput: a = 2, b = 3\nOutput: 5\n\nExample 2:\nInput: a = -4, b = 10\nOutput: 6\n\nConstraints:\n-1000 <= a, b <= 1000",
            Difficulty: 50,
            Tags: ["math", "beginner", "arithmetic"],
            Setups:
            [
                new("javascript", "function sumTwoIntegers(a, b) {\n    \n}", "sumTwoIntegers"),
                new("python", "def sum_two_integers(a: int, b: int) -> int:\n    pass", "sum_two_integers"),
                new(
                    "java",
                    "class Solution {\n    public int sumTwoIntegers(int a, int b) {\n        return 0;\n    }\n}",
                    "sumTwoIntegers"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int sumTwoIntegers(int a, int b) {\n        return 0;\n    }\n};",
                    "sumTwoIntegers"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("2", "integer"), new("3", "integer")], [new("5", "integer")]),
                new("Example 2", [new("-4", "integer"), new("10", "integer")], [new("6", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("0", "integer"), new("0", "integer")], [new("0", "integer")]),
                new("Hidden 2", [new("999", "integer"), new("1", "integer")], [new("1000", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def sum_two_integers(a: int, b: int) -> int:
                    return a + b
                """,
                Parameters:
                [
                    new("a", "integer", Min: -1000, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
                    new("b", "integer", Min: -1000, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 33333
            )
        ),
        new(
            Slug: "is-even",
            Title: "Is Even",
            Question: "Given an integer n, return true if n is even, otherwise return false.\n\nExample 1:\nInput: n = 8\nOutput: true\n\nExample 2:\nInput: n = 7\nOutput: false\n\nConstraints:\n-10^9 <= n <= 10^9",
            Difficulty: 140,
            Tags: ["math", "modulo", "beginner"],
            Setups:
            [
                new("javascript", "function isEven(n) {\n    \n}", "isEven"),
                new("python", "def is_even(n: int) -> bool:\n    pass", "is_even"),
                new(
                    "java",
                    "class Solution {\n    public boolean isEven(int n) {\n        return false;\n    }\n}",
                    "isEven"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    bool isEven(int n) {\n        return false;\n    }\n};",
                    "isEven"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("8", "integer")], [new("true", "boolean")]),
                new("Example 2", [new("7", "integer")], [new("false", "boolean")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("0", "integer")], [new("true", "boolean")]),
                new("Hidden 2", [new("-13", "integer")], [new("false", "boolean")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def is_even(n: int) -> bool:
                    return n % 2 == 0
                """,
                Parameters:
                [
                    new(
                        "n",
                        "integer",
                        Min: -1_000_000_000,
                        Max: 1_000_000_000,
                        LengthMin: null,
                        LengthMax: null,
                        Charset: null
                    ),
                ],
                OutputValueType: "boolean",
                TargetCaseCount: 20,
                Seed: 22222
            )
        ),
        new(
            Slug: "reverse-word",
            Title: "Reverse Word",
            Question: "Given a non-empty string word, return a new string with the characters in reverse order.\n\nExample 1:\nInput: word = \"code\"\nOutput: \"edoc\"\n\nExample 2:\nInput: word = \"racecar\"\nOutput: \"racecar\"\n\nConstraints:\n1 <= word.length <= 100\nword contains only lowercase English letters.",
            Difficulty: 260,
            Tags: ["string", "two-pointers", "easy"],
            Setups:
            [
                new("javascript", "function reverseWord(word) {\n    \n}", "reverseWord"),
                new("python", "def reverse_word(word: str) -> str:\n    pass", "reverse_word"),
                new(
                    "java",
                    "class Solution {\n    public String reverseWord(String word) {\n        return null;\n    }\n}",
                    "reverseWord"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    string reverseWord(string word) {\n        return \"\";\n    }\n};",
                    "reverseWord"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("code", "string")], [new("edoc", "string")]),
                new("Example 2", [new("racecar", "string")], [new("racecar", "string")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("algorithm", "string")], [new("mhtirogla", "string")]),
                new("Hidden 2", [new("a", "string")], [new("a", "string")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def reverse_word(word: str) -> str:
                    return word[::-1]
                """,
                Parameters:
                [
                    new(
                        "word",
                        "string",
                        Min: null,
                        Max: null,
                        LengthMin: 1,
                        LengthMax: 20,
                        Charset: "abcdefghijklmnopqrstuvwxyz"
                    ),
                ],
                OutputValueType: "string",
                TargetCaseCount: 20,
                Seed: 44444
            )
        ),
        new(
            Slug: "max-of-three",
            Title: "Max of Three",
            Question: "Given three integers a, b, and c, return the largest value among them.\n\nExample 1:\nInput: a = 1, b = 7, c = 3\nOutput: 7\n\nExample 2:\nInput: a = -5, b = -2, c = -9\nOutput: -2\n\nConstraints:\n-10^6 <= a, b, c <= 10^6",
            Difficulty: 420,
            Tags: ["math", "comparison", "easy"],
            Setups:
            [
                new("javascript", "function maxOfThree(a, b, c) {\n    \n}", "maxOfThree"),
                new("python", "def max_of_three(a: int, b: int, c: int) -> int:\n    pass", "max_of_three"),
                new(
                    "java",
                    "class Solution {\n    public int maxOfThree(int a, int b, int c) {\n        return 0;\n    }\n}",
                    "maxOfThree"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int maxOfThree(int a, int b, int c) {\n        return 0;\n    }\n};",
                    "maxOfThree"
                ),
            ],
            SampleCases:
            [
                new(
                    "Example 1",
                    [new("1", "integer"), new("7", "integer"), new("3", "integer")],
                    [new("7", "integer")]
                ),
                new(
                    "Example 2",
                    [new("-5", "integer"), new("-2", "integer"), new("-9", "integer")],
                    [new("-2", "integer")]
                ),
            ],
            HiddenCases:
            [
                new(
                    "Hidden 1",
                    [new("10", "integer"), new("10", "integer"), new("9", "integer")],
                    [new("10", "integer")]
                ),
                new(
                    "Hidden 2",
                    [new("100", "integer"), new("250", "integer"), new("249", "integer")],
                    [new("250", "integer")]
                ),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def max_of_three(a: int, b: int, c: int) -> int:
                    return max(a, b, c)
                """,
                Parameters:
                [
                    new(
                        "a",
                        "integer",
                        Min: -1_000_000,
                        Max: 1_000_000,
                        LengthMin: null,
                        LengthMax: null,
                        Charset: null
                    ),
                    new(
                        "b",
                        "integer",
                        Min: -1_000_000,
                        Max: 1_000_000,
                        LengthMin: null,
                        LengthMax: null,
                        Charset: null
                    ),
                    new(
                        "c",
                        "integer",
                        Min: -1_000_000,
                        Max: 1_000_000,
                        LengthMin: null,
                        LengthMax: null,
                        Charset: null
                    ),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 55555
            )
        ),
        new(
            Slug: "count-vowels",
            Title: "Count Vowels",
            Question: "Given a string s consisting of lowercase English letters, return the number of vowels (a, e, i, o, u) in s.\n\nExample 1:\nInput: s = \"hello\"\nOutput: 2\n\nExample 2:\nInput: s = \"rhythm\"\nOutput: 0\n\nConstraints:\n1 <= s.length <= 100\ns consists of lowercase English letters only.",
            Difficulty: 180,
            Tags: ["string", "beginner"],
            Setups:
            [
                new("javascript", "function countVowels(s) {\n    \n}", "countVowels"),
                new("python", "def count_vowels(s: str) -> int:\n    pass", "count_vowels"),
                new(
                    "java",
                    "class Solution {\n    public int countVowels(String s) {\n        return 0;\n    }\n}",
                    "countVowels"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int countVowels(string s) {\n        return 0;\n    }\n};",
                    "countVowels"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("hello", "string")], [new("2", "integer")]),
                new("Example 2", [new("rhythm", "string")], [new("0", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("aeiou", "string")], [new("5", "integer")]),
                new("Hidden 2", [new("bcdfg", "string")], [new("0", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def count_vowels(s: str) -> int:
                    return sum(1 for ch in s if ch in "aeiou")
                """,
                Parameters:
                [
                    new(
                        "s",
                        "string",
                        Min: null,
                        Max: null,
                        LengthMin: 1,
                        LengthMax: 50,
                        Charset: "aeioubcdfghjklmnpqrstvwxyz"
                    ),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 66666
            )
        ),
        new(
            Slug: "array-sum",
            Title: "Array Sum",
            Question: "Given an array of integers nums, return the sum of all elements.\n\nExample 1:\nInput: nums = [1,2,3]\nOutput: 6\n\nExample 2:\nInput: nums = [-4,5,10]\nOutput: 11\n\nConstraints:\n1 <= nums.length <= 100\n-1000 <= nums[i] <= 1000",
            Difficulty: 150,
            Tags: ["array", "beginner", "math"],
            Setups:
            [
                new("javascript", "function arraySum(nums) {\n    \n}", "arraySum"),
                new("python", "def array_sum(nums: list[int]) -> int:\n    pass", "array_sum"),
                new(
                    "java",
                    "class Solution {\n    public int arraySum(int[] nums) {\n        return 0;\n    }\n}",
                    "arraySum"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int arraySum(vector<int>& nums) {\n        return 0;\n    }\n};",
                    "arraySum"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("[1,2,3]", "integer_array")], [new("6", "integer")]),
                new("Example 2", [new("[-4,5,10]", "integer_array")], [new("11", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("[0,0,0]", "integer_array")], [new("0", "integer")]),
                new("Hidden 2", [new("[100,-100,50]", "integer_array")], [new("50", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def array_sum(nums: list[int]) -> int:
                    return sum(nums)
                """,
                Parameters:
                [
                    new("nums", "integer_array", Min: -1000, Max: 1000, LengthMin: 1, LengthMax: 30, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 77777
            )
        ),
        new(
            Slug: "find-max-in-array",
            Title: "Find Maximum In Array",
            Question: "Given a non-empty array of integers nums, return the largest value in the array.\n\nExample 1:\nInput: nums = [3,1,4,1,5]\nOutput: 5\n\nExample 2:\nInput: nums = [-8,-2,-9]\nOutput: -2\n\nConstraints:\n1 <= nums.length <= 100\n-1000 <= nums[i] <= 1000",
            Difficulty: 230,
            Tags: ["array", "easy"],
            Setups:
            [
                new("javascript", "function findMax(nums) {\n    \n}", "findMax"),
                new("python", "def find_max(nums: list[int]) -> int:\n    pass", "find_max"),
                new(
                    "java",
                    "class Solution {\n    public int findMax(int[] nums) {\n        return 0;\n    }\n}",
                    "findMax"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int findMax(vector<int>& nums) {\n        return 0;\n    }\n};",
                    "findMax"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("[3,1,4,1,5]", "integer_array")], [new("5", "integer")]),
                new("Example 2", [new("[-8,-2,-9]", "integer_array")], [new("-2", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("[7]", "integer_array")], [new("7", "integer")]),
                new("Hidden 2", [new("[0,0,0,1]", "integer_array")], [new("1", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def find_max(nums: list[int]) -> int:
                    return max(nums)
                """,
                Parameters:
                [
                    new("nums", "integer_array", Min: -1000, Max: 1000, LengthMin: 1, LengthMax: 30, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 88888
            )
        ),
        new(
            Slug: "palindrome-number",
            Title: "Palindrome Number",
            Question: "Given an integer n, return true if n is a palindrome (reads the same forwards and backwards), otherwise return false. Negative numbers are never palindromes.\n\nExample 1:\nInput: n = 121\nOutput: true\n\nExample 2:\nInput: n = -121\nOutput: false\n\nConstraints:\n-1000000 <= n <= 1000000",
            Difficulty: 320,
            Tags: ["math", "easy"],
            Setups:
            [
                new("javascript", "function isPalindromeNumber(n) {\n    \n}", "isPalindromeNumber"),
                new("python", "def is_palindrome_number(n: int) -> bool:\n    pass", "is_palindrome_number"),
                new(
                    "java",
                    "class Solution {\n    public boolean isPalindromeNumber(int n) {\n        return false;\n    }\n}",
                    "isPalindromeNumber"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    bool isPalindromeNumber(int n) {\n        return false;\n    }\n};",
                    "isPalindromeNumber"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("121", "integer")], [new("true", "boolean")]),
                new("Example 2", [new("-121", "integer")], [new("false", "boolean")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("0", "integer")], [new("true", "boolean")]),
                new("Hidden 2", [new("123", "integer")], [new("false", "boolean")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def is_palindrome_number(n: int) -> bool:
                    if n < 0:
                        return False
                    s = str(n)
                    return s == s[::-1]
                """,
                Parameters:
                [
                    new(
                        "n",
                        "integer",
                        Min: -1_000_000,
                        Max: 1_000_000,
                        LengthMin: null,
                        LengthMax: null,
                        Charset: null
                    ),
                ],
                OutputValueType: "boolean",
                TargetCaseCount: 20,
                Seed: 99999
            )
        ),
        new(
            Slug: "second-largest",
            Title: "Second Largest",
            Question: "Given an array of integers nums containing at least two distinct values, return the second largest distinct value.\n\nExample 1:\nInput: nums = [4,2,9,9,5]\nOutput: 5\n\nExample 2:\nInput: nums = [1,1,2]\nOutput: 1\n\nConstraints:\n2 <= nums.length <= 100\n-1000 <= nums[i] <= 1000\nnums contains at least two distinct values.",
            Difficulty: 700,
            Tags: ["array", "intermediate"],
            Setups:
            [
                new("javascript", "function secondLargest(nums) {\n    \n}", "secondLargest"),
                new("python", "def second_largest(nums: list[int]) -> int:\n    pass", "second_largest"),
                new(
                    "java",
                    "class Solution {\n    public int secondLargest(int[] nums) {\n        return 0;\n    }\n}",
                    "secondLargest"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int secondLargest(vector<int>& nums) {\n        return 0;\n    }\n};",
                    "secondLargest"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("[4,2,9,9,5]", "integer_array")], [new("5", "integer")]),
                new("Example 2", [new("[1,1,2]", "integer_array")], [new("1", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("[10,20]", "integer_array")], [new("10", "integer")]),
                new("Hidden 2", [new("[-5,-1,-1,-9]", "integer_array")], [new("-5", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def second_largest(nums: list[int]) -> int:
                    unique = sorted(set(nums), reverse=True)
                    if len(unique) < 2:
                        raise ValueError("no second largest value exists")
                    return unique[1]
                """,
                Parameters:
                [
                    new("nums", "integer_array", Min: -100, Max: 100, LengthMin: 5, LengthMax: 15, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 10101
            )
        ),
        new(
            Slug: "multiply-two-integers",
            Title: "Multiply Two Integers",
            Question: "Given two integers a and b, return the product a * b.\n\nExample 1:\nInput: a = 3, b = 4\nOutput: 12\n\nExample 2:\nInput: a = -2, b = 5\nOutput: -10\n\nConstraints:\n-1000 <= a, b <= 1000",
            Difficulty: 60,
            Tags: ["math", "beginner", "arithmetic"],
            Setups:
            [
                new("javascript", "function multiplyTwoIntegers(a, b) {\n    \n}", "multiplyTwoIntegers"),
                new("python", "def multiply_two_integers(a: int, b: int) -> int:\n    pass", "multiply_two_integers"),
                new(
                    "java",
                    "class Solution {\n    public int multiplyTwoIntegers(int a, int b) {\n        return 0;\n    }\n}",
                    "multiplyTwoIntegers"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int multiplyTwoIntegers(int a, int b) {\n        return 0;\n    }\n};",
                    "multiplyTwoIntegers"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("3", "integer"), new("4", "integer")], [new("12", "integer")]),
                new("Example 2", [new("-2", "integer"), new("5", "integer")], [new("-10", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("0", "integer"), new("100", "integer")], [new("0", "integer")]),
                new("Hidden 2", [new("-7", "integer"), new("-8", "integer")], [new("56", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def multiply_two_integers(a: int, b: int) -> int:
                    return a * b
                """,
                Parameters:
                [
                    new("a", "integer", Min: -1000, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
                    new("b", "integer", Min: -1000, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 13131
            )
        ),
        new(
            Slug: "is-positive",
            Title: "Is Positive",
            Question: "Given an integer n, return true if n is strictly greater than 0, otherwise return false.\n\nExample 1:\nInput: n = 5\nOutput: true\n\nExample 2:\nInput: n = -3\nOutput: false\n\nConstraints:\n-10^9 <= n <= 10^9",
            Difficulty: 40,
            Tags: ["math", "comparison", "beginner"],
            Setups:
            [
                new("javascript", "function isPositive(n) {\n    \n}", "isPositive"),
                new("python", "def is_positive(n: int) -> bool:\n    pass", "is_positive"),
                new(
                    "java",
                    "class Solution {\n    public boolean isPositive(int n) {\n        return false;\n    }\n}",
                    "isPositive"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    bool isPositive(int n) {\n        return false;\n    }\n};",
                    "isPositive"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("5", "integer")], [new("true", "boolean")]),
                new("Example 2", [new("-3", "integer")], [new("false", "boolean")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("0", "integer")], [new("false", "boolean")]),
                new("Hidden 2", [new("1", "integer")], [new("true", "boolean")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def is_positive(n: int) -> bool:
                    return n > 0
                """,
                Parameters:
                [
                    new(
                        "n",
                        "integer",
                        Min: -1_000_000_000,
                        Max: 1_000_000_000,
                        LengthMin: null,
                        LengthMax: null,
                        Charset: null
                    ),
                ],
                OutputValueType: "boolean",
                TargetCaseCount: 20,
                Seed: 14141
            )
        ),
        new(
            Slug: "count-consonants",
            Title: "Count Consonants",
            Question: "Given a string s consisting of lowercase English letters, return the number of consonants (every letter except a, e, i, o, u) in s.\n\nExample 1:\nInput: s = \"hello\"\nOutput: 3\n\nExample 2:\nInput: s = \"aeiou\"\nOutput: 0\n\nConstraints:\n1 <= s.length <= 100\ns consists of lowercase English letters only.",
            Difficulty: 190,
            Tags: ["string", "beginner"],
            Setups:
            [
                new("javascript", "function countConsonants(s) {\n    \n}", "countConsonants"),
                new("python", "def count_consonants(s: str) -> int:\n    pass", "count_consonants"),
                new(
                    "java",
                    "class Solution {\n    public int countConsonants(String s) {\n        return 0;\n    }\n}",
                    "countConsonants"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int countConsonants(string s) {\n        return 0;\n    }\n};",
                    "countConsonants"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("hello", "string")], [new("3", "integer")]),
                new("Example 2", [new("aeiou", "string")], [new("0", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("bcdfg", "string")], [new("5", "integer")]),
                new("Hidden 2", [new("rhythm", "string")], [new("6", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def count_consonants(s: str) -> int:
                    return sum(1 for ch in s if ch not in "aeiou")
                """,
                Parameters:
                [
                    new(
                        "s",
                        "string",
                        Min: null,
                        Max: null,
                        LengthMin: 1,
                        LengthMax: 50,
                        Charset: "aeioubcdfghjklmnpqrstvwxyz"
                    ),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 15151
            )
        ),
        new(
            Slug: "average-of-array",
            Title: "Average Of Array",
            Question: "Given a non-empty array of integers nums, return the average of all elements as a floating-point number.\n\nExample 1:\nInput: nums = [1,2,3]\nOutput: 2.0\n\nExample 2:\nInput: nums = [4,8,15,16]\nOutput: 10.75\n\nConstraints:\n1 <= nums.length <= 100\n-1000 <= nums[i] <= 1000",
            Difficulty: 260,
            Tags: ["array", "math", "easy"],
            Setups:
            [
                new("javascript", "function averageOfArray(nums) {\n    \n}", "averageOfArray"),
                new("python", "def average_of_array(nums: list[int]) -> float:\n    pass", "average_of_array"),
                new(
                    "java",
                    "class Solution {\n    public double averageOfArray(int[] nums) {\n        return 0;\n    }\n}",
                    "averageOfArray"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    double averageOfArray(vector<int>& nums) {\n        return 0;\n    }\n};",
                    "averageOfArray"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("[1,2,3]", "integer_array")], [new("2.0", "double")]),
                new("Example 2", [new("[4,8,15,16]", "integer_array")], [new("10.75", "double")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("[100]", "integer_array")], [new("100.0", "double")]),
                new("Hidden 2", [new("[1,2]", "integer_array")], [new("1.5", "double")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def average_of_array(nums: list[int]) -> float:
                    return sum(nums) / len(nums)
                """,
                Parameters:
                [
                    new("nums", "integer_array", Min: -1000, Max: 1000, LengthMin: 1, LengthMax: 30, Charset: null),
                ],
                OutputValueType: "double",
                TargetCaseCount: 20,
                Seed: 16161
            )
        ),
        new(
            Slug: "find-min-in-array",
            Title: "Find Minimum In Array",
            Question: "Given a non-empty array of integers nums, return the smallest value in the array.\n\nExample 1:\nInput: nums = [3,1,4,1,5]\nOutput: 1\n\nExample 2:\nInput: nums = [-8,-2,-9]\nOutput: -9\n\nConstraints:\n1 <= nums.length <= 100\n-1000 <= nums[i] <= 1000",
            Difficulty: 235,
            Tags: ["array", "easy"],
            Setups:
            [
                new("javascript", "function findMin(nums) {\n    \n}", "findMin"),
                new("python", "def find_min(nums: list[int]) -> int:\n    pass", "find_min"),
                new(
                    "java",
                    "class Solution {\n    public int findMin(int[] nums) {\n        return 0;\n    }\n}",
                    "findMin"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int findMin(vector<int>& nums) {\n        return 0;\n    }\n};",
                    "findMin"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("[3,1,4,1,5]", "integer_array")], [new("1", "integer")]),
                new("Example 2", [new("[-8,-2,-9]", "integer_array")], [new("-9", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("[7]", "integer_array")], [new("7", "integer")]),
                new("Hidden 2", [new("[0,0,0,-1]", "integer_array")], [new("-1", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def find_min(nums: list[int]) -> int:
                    return min(nums)
                """,
                Parameters:
                [
                    new("nums", "integer_array", Min: -1000, Max: 1000, LengthMin: 1, LengthMax: 30, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 17171
            )
        ),
        new(
            Slug: "is-prime",
            Title: "Is Prime",
            Question: "Given a positive integer n, return true if n is a prime number, otherwise return false.\n\nExample 1:\nInput: n = 7\nOutput: true\n\nExample 2:\nInput: n = 8\nOutput: false\n\nConstraints:\n1 <= n <= 1000000",
            Difficulty: 560,
            Tags: ["math", "number-theory", "intermediate"],
            Setups:
            [
                new("javascript", "function isPrime(n) {\n    \n}", "isPrime"),
                new("python", "def is_prime(n: int) -> bool:\n    pass", "is_prime"),
                new(
                    "java",
                    "class Solution {\n    public boolean isPrime(int n) {\n        return false;\n    }\n}",
                    "isPrime"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    bool isPrime(int n) {\n        return false;\n    }\n};",
                    "isPrime"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("7", "integer")], [new("true", "boolean")]),
                new("Example 2", [new("8", "integer")], [new("false", "boolean")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("1", "integer")], [new("false", "boolean")]),
                new("Hidden 2", [new("2", "integer")], [new("true", "boolean")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def is_prime(n: int) -> bool:
                    if n < 2:
                        return False
                    i = 2
                    while i * i <= n:
                        if n % i == 0:
                            return False
                        i += 1
                    return True
                """,
                Parameters:
                [
                    new("n", "integer", Min: 1, Max: 1_000_000, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "boolean",
                TargetCaseCount: 20,
                Seed: 18181
            )
        ),
        new(
            Slug: "fibonacci-number",
            Title: "Fibonacci Number",
            Question: "Given an integer n, return the nth Fibonacci number, where fib(0) = 0, fib(1) = 1, and fib(n) = fib(n-1) + fib(n-2) for n > 1.\n\nExample 1:\nInput: n = 5\nOutput: 5\n\nExample 2:\nInput: n = 10\nOutput: 55\n\nConstraints:\n0 <= n <= 40",
            Difficulty: 610,
            Tags: ["math", "dynamic-programming", "intermediate"],
            Setups:
            [
                new("javascript", "function fibonacciNumber(n) {\n    \n}", "fibonacciNumber"),
                new("python", "def fibonacci_number(n: int) -> int:\n    pass", "fibonacci_number"),
                new(
                    "java",
                    "class Solution {\n    public int fibonacciNumber(int n) {\n        return 0;\n    }\n}",
                    "fibonacciNumber"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int fibonacciNumber(int n) {\n        return 0;\n    }\n};",
                    "fibonacciNumber"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("5", "integer")], [new("5", "integer")]),
                new("Example 2", [new("10", "integer")], [new("55", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("0", "integer")], [new("0", "integer")]),
                new("Hidden 2", [new("1", "integer")], [new("1", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def fibonacci_number(n: int) -> int:
                    a, b = 0, 1
                    for _ in range(n):
                        a, b = b, a + b
                    return a
                """,
                Parameters: [new("n", "integer", Min: 0, Max: 40, LengthMin: null, LengthMax: null, Charset: null)],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 19191
            )
        ),
        new(
            Slug: "string-length",
            Title: "String Length",
            Question: "Given a string s, return the number of characters in s.\n\nExample 1:\nInput: s = \"hello\"\nOutput: 5\n\nExample 2:\nInput: s = \"zoo\"\nOutput: 3\n\nConstraints:\n1 <= s.length <= 1000",
            Difficulty: 20,
            Tags: ["string", "beginner"],
            Setups:
            [
                new("javascript", "function stringLength(s) {\n    \n}", "stringLength"),
                new("python", "def string_length(s: str) -> int:\n    pass", "string_length"),
                new(
                    "java",
                    "class Solution {\n    public int stringLength(String s) {\n        return 0;\n    }\n}",
                    "stringLength"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int stringLength(string s) {\n        return 0;\n    }\n};",
                    "stringLength"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("hello", "string")], [new("5", "integer")]),
                new("Example 2", [new("zoo", "string")], [new("3", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("a", "string")], [new("1", "integer")]),
                new("Hidden 2", [new("algorithm", "string")], [new("9", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def string_length(s: str) -> int:
                    return len(s)
                """,
                Parameters:
                [
                    new(
                        "s",
                        "string",
                        Min: null,
                        Max: null,
                        LengthMin: 1,
                        LengthMax: 50,
                        Charset: "abcdefghijklmnopqrstuvwxyz"
                    ),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 20202
            )
        ),
        new(
            Slug: "contains-duplicate",
            Title: "Contains Duplicate",
            Question: "Given an array of integers nums, return true if any value appears at least twice in the array, and return false if every element is distinct.\n\nExample 1:\nInput: nums = [1,2,3,1]\nOutput: true\n\nExample 2:\nInput: nums = [1,2,3,4]\nOutput: false\n\nConstraints:\n1 <= nums.length <= 100\n-1000 <= nums[i] <= 1000",
            Difficulty: 350,
            Tags: ["array", "hash-table", "easy"],
            Setups:
            [
                new("javascript", "function containsDuplicate(nums) {\n    \n}", "containsDuplicate"),
                new("python", "def contains_duplicate(nums: list[int]) -> bool:\n    pass", "contains_duplicate"),
                new(
                    "java",
                    "class Solution {\n    public boolean containsDuplicate(int[] nums) {\n        return false;\n    }\n}",
                    "containsDuplicate"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    bool containsDuplicate(vector<int>& nums) {\n        return false;\n    }\n};",
                    "containsDuplicate"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("[1,2,3,1]", "integer_array")], [new("true", "boolean")]),
                new("Example 2", [new("[1,2,3,4]", "integer_array")], [new("false", "boolean")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("[5]", "integer_array")], [new("false", "boolean")]),
                new("Hidden 2", [new("[0,0]", "integer_array")], [new("true", "boolean")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def contains_duplicate(nums: list[int]) -> bool:
                    return len(nums) != len(set(nums))
                """,
                Parameters:
                [
                    new("nums", "integer_array", Min: -50, Max: 50, LengthMin: 1, LengthMax: 20, Charset: null),
                ],
                OutputValueType: "boolean",
                TargetCaseCount: 20,
                Seed: 21212
            )
        ),
        new(
            Slug: "capitalize-first-letter",
            Title: "Capitalize First Letter",
            Question: "Given a non-empty string word consisting of lowercase English letters, return a new string with the first letter capitalized and the rest unchanged.\n\nExample 1:\nInput: word = \"apple\"\nOutput: \"Apple\"\n\nExample 2:\nInput: word = \"zoo\"\nOutput: \"Zoo\"\n\nConstraints:\n1 <= word.length <= 100\nword consists of lowercase English letters only.",
            Difficulty: 210,
            Tags: ["string", "easy"],
            Setups:
            [
                new("javascript", "function capitalizeFirstLetter(word) {\n    \n}", "capitalizeFirstLetter"),
                new("python", "def capitalize_first_letter(word: str) -> str:\n    pass", "capitalize_first_letter"),
                new(
                    "java",
                    "class Solution {\n    public String capitalizeFirstLetter(String word) {\n        return null;\n    }\n}",
                    "capitalizeFirstLetter"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    string capitalizeFirstLetter(string word) {\n        return \"\";\n    }\n};",
                    "capitalizeFirstLetter"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("apple", "string")], [new("Apple", "string")]),
                new("Example 2", [new("zoo", "string")], [new("Zoo", "string")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("a", "string")], [new("A", "string")]),
                new("Hidden 2", [new("algorithm", "string")], [new("Algorithm", "string")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def capitalize_first_letter(word: str) -> str:
                    return word[0].upper() + word[1:]
                """,
                Parameters:
                [
                    new(
                        "word",
                        "string",
                        Min: null,
                        Max: null,
                        LengthMin: 1,
                        LengthMax: 30,
                        Charset: "abcdefghijklmnopqrstuvwxyz"
                    ),
                ],
                OutputValueType: "string",
                TargetCaseCount: 20,
                Seed: 23232
            )
        ),
        new(
            Slug: "absolute-difference",
            Title: "Absolute Difference",
            Question: "Given two integers a and b, return the absolute value of their difference.\n\nExample 1:\nInput: a = 10, b = 3\nOutput: 7\n\nExample 2:\nInput: a = -5, b = 5\nOutput: 10\n\nConstraints:\n-1000 <= a, b <= 1000",
            Difficulty: 65,
            Tags: ["math", "beginner", "arithmetic"],
            Setups:
            [
                new("javascript", "function absoluteDifference(a, b) {\n    \n}", "absoluteDifference"),
                new("python", "def absolute_difference(a: int, b: int) -> int:\n    pass", "absolute_difference"),
                new(
                    "java",
                    "class Solution {\n    public int absoluteDifference(int a, int b) {\n        return 0;\n    }\n}",
                    "absoluteDifference"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int absoluteDifference(int a, int b) {\n        return 0;\n    }\n};",
                    "absoluteDifference"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("10", "integer"), new("3", "integer")], [new("7", "integer")]),
                new("Example 2", [new("-5", "integer"), new("5", "integer")], [new("10", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("0", "integer"), new("0", "integer")], [new("0", "integer")]),
                new("Hidden 2", [new("-1000", "integer"), new("1000", "integer")], [new("2000", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def absolute_difference(a: int, b: int) -> int:
                    return abs(a - b)
                """,
                Parameters:
                [
                    new("a", "integer", Min: -1000, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
                    new("b", "integer", Min: -1000, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 24242
            )
        ),
        new(
            Slug: "celsius-to-fahrenheit",
            Title: "Celsius To Fahrenheit",
            Question: "Given a temperature celsius in degrees Celsius, return the equivalent temperature in degrees Fahrenheit using the formula F = (C * 9/5) + 32.\n\nExample 1:\nInput: celsius = 0\nOutput: 32.0\n\nExample 2:\nInput: celsius = 100\nOutput: 212.0\n\nConstraints:\n-273 <= celsius <= 1000",
            Difficulty: 85,
            Tags: ["math", "conversion", "beginner"],
            Setups:
            [
                new("javascript", "function celsiusToFahrenheit(celsius) {\n    \n}", "celsiusToFahrenheit"),
                new("python", "def celsius_to_fahrenheit(celsius: float) -> float:\n    pass", "celsius_to_fahrenheit"),
                new(
                    "java",
                    "class Solution {\n    public double celsiusToFahrenheit(double celsius) {\n        return 0;\n    }\n}",
                    "celsiusToFahrenheit"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    double celsiusToFahrenheit(double celsius) {\n        return 0;\n    }\n};",
                    "celsiusToFahrenheit"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("0", "double")], [new("32.0", "double")]),
                new("Example 2", [new("100", "double")], [new("212.0", "double")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("-40", "double")], [new("-40.0", "double")]),
                new("Hidden 2", [new("37", "double")], [new("98.6", "double")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def celsius_to_fahrenheit(celsius: float) -> float:
                    return celsius * 9 / 5 + 32
                """,
                Parameters:
                [
                    new("celsius", "double", Min: -273, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "double",
                TargetCaseCount: 20,
                Seed: 25252
            )
        ),
        new(
            Slug: FactorialFunctionName,
            Title: "Factorial",
            Question: "Given a non-negative integer n, return n! (n factorial), the product of all positive integers less than or equal to n. By definition, 0! = 1.\n\nExample 1:\nInput: n = 5\nOutput: 120\n\nExample 2:\nInput: n = 0\nOutput: 1\n\nConstraints:\n0 <= n <= 12",
            Difficulty: 240,
            Tags: ["math", "recursion", "easy"],
            Setups:
            [
                new("javascript", "function factorial(n) {\n    \n}", FactorialFunctionName),
                new("python", "def factorial(n: int) -> int:\n    pass", FactorialFunctionName),
                new(
                    "java",
                    "class Solution {\n    public int factorial(int n) {\n        return 0;\n    }\n}",
                    FactorialFunctionName
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int factorial(int n) {\n        return 0;\n    }\n};",
                    FactorialFunctionName
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("5", "integer")], [new("120", "integer")]),
                new("Example 2", [new("0", "integer")], [new("1", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("1", "integer")], [new("1", "integer")]),
                new("Hidden 2", [new("10", "integer")], [new("3628800", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def factorial(n: int) -> int:
                    result = 1
                    for i in range(2, n + 1):
                        result *= i
                    return result
                """,
                Parameters: [new("n", "integer", Min: 0, Max: 12, LengthMin: null, LengthMax: null, Charset: null)],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 26262
            )
        ),
        new(
            Slug: "is-leap-year",
            Title: "Is Leap Year",
            Question: "Given an integer year, return true if year is a leap year in the Gregorian calendar, otherwise return false. A year is a leap year if it is divisible by 4, except for years divisible by 100 unless also divisible by 400.\n\nExample 1:\nInput: year = 2024\nOutput: true\n\nExample 2:\nInput: year = 1900\nOutput: false\n\nConstraints:\n1 <= year <= 9999",
            Difficulty: 150,
            Tags: ["math", "conditionals", "beginner"],
            Setups:
            [
                new("javascript", "function isLeapYear(year) {\n    \n}", "isLeapYear"),
                new("python", "def is_leap_year(year: int) -> bool:\n    pass", "is_leap_year"),
                new(
                    "java",
                    "class Solution {\n    public boolean isLeapYear(int year) {\n        return false;\n    }\n}",
                    "isLeapYear"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    bool isLeapYear(int year) {\n        return false;\n    }\n};",
                    "isLeapYear"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("2024", "integer")], [new("true", "boolean")]),
                new("Example 2", [new("1900", "integer")], [new("false", "boolean")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("2000", "integer")], [new("true", "boolean")]),
                new("Hidden 2", [new("2023", "integer")], [new("false", "boolean")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def is_leap_year(year: int) -> bool:
                    return year % 4 == 0 and (year % 100 != 0 or year % 400 == 0)
                """,
                Parameters:
                [
                    new("year", "integer", Min: 1, Max: 9999, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "boolean",
                TargetCaseCount: 20,
                Seed: 27272
            )
        ),
        new(
            Slug: "digit-sum",
            Title: "Digit Sum",
            Question: "Given a non-negative integer n, return the sum of its digits.\n\nExample 1:\nInput: n = 1234\nOutput: 10\n\nExample 2:\nInput: n = 7\nOutput: 7\n\nConstraints:\n0 <= n <= 1000000000",
            Difficulty: 175,
            Tags: ["math", "string", "beginner"],
            Setups:
            [
                new("javascript", "function digitSum(n) {\n    \n}", "digitSum"),
                new("python", "def digit_sum(n: int) -> int:\n    pass", "digit_sum"),
                new(
                    "java",
                    "class Solution {\n    public int digitSum(int n) {\n        return 0;\n    }\n}",
                    "digitSum"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int digitSum(int n) {\n        return 0;\n    }\n};",
                    "digitSum"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("1234", "integer")], [new("10", "integer")]),
                new("Example 2", [new("7", "integer")], [new("7", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("0", "integer")], [new("0", "integer")]),
                new("Hidden 2", [new("999999", "integer")], [new("54", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def digit_sum(n: int) -> int:
                    return sum(int(d) for d in str(n))
                """,
                Parameters:
                [
                    new("n", "integer", Min: 0, Max: 1_000_000_000, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 28282
            )
        ),
        new(
            Slug: "count-occurrences",
            Title: "Count Occurrences",
            Question: "Given an array of integers nums and an integer target, return the number of times target appears in nums.\n\nExample 1:\nInput: nums = [1,2,2,3,2], target = 2\nOutput: 3\n\nExample 2:\nInput: nums = [5,5,5], target = 1\nOutput: 0\n\nConstraints:\n1 <= nums.length <= 100\n-1000 <= nums[i], target <= 1000",
            Difficulty: 165,
            Tags: ["array", "easy"],
            Setups:
            [
                new("javascript", "function countOccurrences(nums, target) {\n    \n}", "countOccurrences"),
                new(
                    "python",
                    "def count_occurrences(nums: list[int], target: int) -> int:\n    pass",
                    "count_occurrences"
                ),
                new(
                    "java",
                    "class Solution {\n    public int countOccurrences(int[] nums, int target) {\n        return 0;\n    }\n}",
                    "countOccurrences"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int countOccurrences(vector<int>& nums, int target) {\n        return 0;\n    }\n};",
                    "countOccurrences"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("[1,2,2,3,2]", "integer_array"), new("2", "integer")], [new("3", "integer")]),
                new("Example 2", [new("[5,5,5]", "integer_array"), new("1", "integer")], [new("0", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("[0]", "integer_array"), new("0", "integer")], [new("1", "integer")]),
                new("Hidden 2", [new("[-1,-1,2,3]", "integer_array"), new("-1", "integer")], [new("2", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def count_occurrences(nums: list[int], target: int) -> int:
                    return nums.count(target)
                """,
                Parameters:
                [
                    new("nums", "integer_array", Min: -20, Max: 20, LengthMin: 1, LengthMax: 30, Charset: null),
                    new("target", "integer", Min: -20, Max: 20, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 29292
            )
        ),
        new(
            Slug: "sum-of-even-numbers",
            Title: "Sum Of Even Numbers",
            Question: "Given an array of integers nums, return the sum of all even numbers in the array.\n\nExample 1:\nInput: nums = [1,2,3,4,5,6]\nOutput: 12\n\nExample 2:\nInput: nums = [1,3,5]\nOutput: 0\n\nConstraints:\n1 <= nums.length <= 100\n-1000 <= nums[i] <= 1000",
            Difficulty: 195,
            Tags: ["array", "math", "easy"],
            Setups:
            [
                new("javascript", "function sumOfEvenNumbers(nums) {\n    \n}", "sumOfEvenNumbers"),
                new("python", "def sum_of_even_numbers(nums: list[int]) -> int:\n    pass", "sum_of_even_numbers"),
                new(
                    "java",
                    "class Solution {\n    public int sumOfEvenNumbers(int[] nums) {\n        return 0;\n    }\n}",
                    "sumOfEvenNumbers"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int sumOfEvenNumbers(vector<int>& nums) {\n        return 0;\n    }\n};",
                    "sumOfEvenNumbers"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("[1,2,3,4,5,6]", "integer_array")], [new("12", "integer")]),
                new("Example 2", [new("[1,3,5]", "integer_array")], [new("0", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("[2,4,6]", "integer_array")], [new("12", "integer")]),
                new("Hidden 2", [new("[-2,-3,5]", "integer_array")], [new("-2", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def sum_of_even_numbers(nums: list[int]) -> int:
                    return sum(n for n in nums if n % 2 == 0)
                """,
                Parameters:
                [
                    new("nums", "integer_array", Min: -1000, Max: 1000, LengthMin: 1, LengthMax: 30, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 30303
            )
        ),
        new(
            Slug: "power-of-two",
            Title: "Power Of Two",
            Question: "Given an integer n, return true if n is a power of two (that is, n = 2^k for some non-negative integer k), otherwise return false.\n\nExample 1:\nInput: n = 16\nOutput: true\n\nExample 2:\nInput: n = 18\nOutput: false\n\nConstraints:\n-1000000 <= n <= 1000000",
            Difficulty: 250,
            Tags: ["math", "bit-manipulation", "easy"],
            Setups:
            [
                new("javascript", "function isPowerOfTwo(n) {\n    \n}", "isPowerOfTwo"),
                new("python", "def is_power_of_two(n: int) -> bool:\n    pass", "is_power_of_two"),
                new(
                    "java",
                    "class Solution {\n    public boolean isPowerOfTwo(int n) {\n        return false;\n    }\n}",
                    "isPowerOfTwo"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    bool isPowerOfTwo(int n) {\n        return false;\n    }\n};",
                    "isPowerOfTwo"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("16", "integer")], [new("true", "boolean")]),
                new("Example 2", [new("18", "integer")], [new("false", "boolean")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("1", "integer")], [new("true", "boolean")]),
                new("Hidden 2", [new("0", "integer")], [new("false", "boolean")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def is_power_of_two(n: int) -> bool:
                    return n > 0 and (n & (n - 1)) == 0
                """,
                Parameters:
                [
                    new(
                        "n",
                        "integer",
                        Min: -1_000_000,
                        Max: 1_000_000,
                        LengthMin: null,
                        LengthMax: null,
                        Charset: null
                    ),
                ],
                OutputValueType: "boolean",
                TargetCaseCount: 20,
                Seed: 31313
            )
        ),
        new(
            Slug: "gcd-of-two-numbers",
            Title: "GCD Of Two Numbers",
            Question: "Given two positive integers a and b, return their greatest common divisor (GCD).\n\nExample 1:\nInput: a = 12, b = 18\nOutput: 6\n\nExample 2:\nInput: a = 17, b = 5\nOutput: 1\n\nConstraints:\n1 <= a, b <= 1000000",
            Difficulty: 330,
            Tags: ["math", "number-theory", "easy"],
            Setups:
            [
                new("javascript", "function gcdOfTwoNumbers(a, b) {\n    \n}", "gcdOfTwoNumbers"),
                new("python", "def gcd_of_two_numbers(a: int, b: int) -> int:\n    pass", "gcd_of_two_numbers"),
                new(
                    "java",
                    "class Solution {\n    public int gcdOfTwoNumbers(int a, int b) {\n        return 0;\n    }\n}",
                    "gcdOfTwoNumbers"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    int gcdOfTwoNumbers(int a, int b) {\n        return 0;\n    }\n};",
                    "gcdOfTwoNumbers"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("12", "integer"), new("18", "integer")], [new("6", "integer")]),
                new("Example 2", [new("17", "integer"), new("5", "integer")], [new("1", "integer")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("100", "integer"), new("75", "integer")], [new("25", "integer")]),
                new("Hidden 2", [new("7", "integer"), new("7", "integer")], [new("7", "integer")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def gcd_of_two_numbers(a: int, b: int) -> int:
                    while b:
                        a, b = b, a % b
                    return a
                """,
                Parameters:
                [
                    new("a", "integer", Min: 1, Max: 1_000_000, LengthMin: null, LengthMax: null, Charset: null),
                    new("b", "integer", Min: 1, Max: 1_000_000, LengthMin: null, LengthMax: null, Charset: null),
                ],
                OutputValueType: "integer",
                TargetCaseCount: 20,
                Seed: 32323
            )
        ),
        new(
            Slug: "valid-anagram",
            Title: "Valid Anagram",
            Question: "Given two strings s and t, return true if t is an anagram of s (the same characters with the same frequencies, in any order), otherwise return false.\n\nExample 1:\nInput: s = \"listen\", t = \"silent\"\nOutput: true\n\nExample 2:\nInput: s = \"rat\", t = \"car\"\nOutput: false\n\nConstraints:\n1 <= s.length, t.length <= 30\ns and t consist of lowercase English letters.",
            Difficulty: 390,
            Tags: ["string", "hash-table", "easy"],
            Setups:
            [
                new("javascript", "function isAnagram(s, t) {\n    \n}", "isAnagram"),
                new("python", "def is_anagram(s: str, t: str) -> bool:\n    pass", "is_anagram"),
                new(
                    "java",
                    "class Solution {\n    public boolean isAnagram(String s, String t) {\n        return false;\n    }\n}",
                    "isAnagram"
                ),
                new(
                    "cpp",
                    "class Solution {\npublic:\n    bool isAnagram(string s, string t) {\n        return false;\n    }\n};",
                    "isAnagram"
                ),
            ],
            SampleCases:
            [
                new("Example 1", [new("listen", "string"), new("silent", "string")], [new("true", "boolean")]),
                new("Example 2", [new("rat", "string"), new("car", "string")], [new("false", "boolean")]),
            ],
            HiddenCases:
            [
                new("Hidden 1", [new("a", "string"), new("a", "string")], [new("true", "boolean")]),
                new("Hidden 2", [new("ab", "string"), new("a", "string")], [new("false", "boolean")]),
            ],
            Generation: new TestCaseGenerationSeed(
                ReferenceSolutionCode: """
                def is_anagram(s: str, t: str) -> bool:
                    return sorted(s) == sorted(t)
                """,
                Parameters:
                [
                    new(
                        "s",
                        "string",
                        Min: null,
                        Max: null,
                        LengthMin: 1,
                        LengthMax: 15,
                        Charset: "abcdefghijklmnopqrstuvwxyz"
                    ),
                    new(
                        "t",
                        "string",
                        Min: null,
                        Max: null,
                        LengthMin: 1,
                        LengthMax: 15,
                        Charset: "abcdefghijklmnopqrstuvwxyz"
                    ),
                ],
                OutputValueType: "boolean",
                TargetCaseCount: 20,
                Seed: 34343
            )
        ),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Guid pipelineId = await pipelineSeeder.GetOrCreateAsync(cancellationToken);

        Dictionary<string, LanguageVersionEntry> versionsBySlug = new(StringComparer.OrdinalIgnoreCase)
        {
            ["javascript"] = await GetVersionAsync("javascript", cancellationToken),
            ["python"] = await GetVersionAsync("python", cancellationToken),
            ["java"] = await GetVersionAsync("java", cancellationToken),
            ["cpp"] = await GetVersionAsync("cpp", cancellationToken),
        };

        foreach (ProblemSeedDefinition problemSeed in ProblemSeeds)
        {
            Guid problemId = await EnsureProblemWithSetupsAsync(
                problemSeed,
                versionsBySlug,
                pipelineId,
                cancellationToken
            );
            await EnsureTestSuitesLinkedAsync(problemId, problemSeed, cancellationToken);
            await EnsureTagsLinkedAsync(problemId, problemSeed.Tags, cancellationToken);

            if (problemSeed.Generation is not null)
            {
                context.ChangeTracker.Clear();
                await EnsureGeneratedTestCasesAsync(problemId, problemSeed, cancellationToken);
            }
        }
    }

    private async Task EnsureGeneratedTestCasesAsync(
        Guid problemId,
        ProblemSeedDefinition problemSeed,
        CancellationToken cancellationToken
    )
    {
        TestCaseGenerationSeed generation = problemSeed.Generation!;

        LanguageVersionEntry pyVersion = await GetVersionAsync("python", cancellationToken);

        var setupInfo = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .Where(s => EF.Property<Guid>(s, "problem_id") == problemId && s.LanguageVersionId == pyVersion.Id)
            .Select(s => new { s.Id, s.GenerationSpecId })
            .FirstOrDefaultAsync(cancellationToken);

        if (setupInfo is null)
            return;

        await TestCaseGenerationSeederHelper.EnqueueIfNeededAsync(
            context,
            testCaseGenerationJobRepository,
            problemSeed.Title,
            setupInfo.Id,
            setupInfo.GenerationSpecId,
            generation.ReferenceSolutionCode,
            generation.Parameters,
            generation.OutputValueType,
            generation.TargetCaseCount,
            generation.Seed,
            cancellationToken
        );

        context.ChangeTracker.Clear();
    }

    private async Task<Guid> EnsureProblemWithSetupsAsync(
        ProblemSeedDefinition problemSeed,
        IReadOnlyDictionary<string, LanguageVersionEntry> versionsBySlug,
        Guid pipelineId,
        CancellationToken cancellationToken
    )
    {
        Guid? existingId = await context
            .Problems.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(problem => problem.Slug.Value == problemSeed.Slug)
            .Select(problem => (Guid?)problem.Id)
            .FirstOrDefaultAsync(cancellationToken);

        List<(Guid versionId, string code, string functionName)> desiredSetups =
        [
            .. problemSeed.Setups.Select(setup =>
                (versionsBySlug[setup.LanguageSlug].Id, setup.Code, setup.FunctionName)
            ),
        ];

        if (existingId is null)
        {
            Problem problem = new(
                new Slug(problemSeed.Slug),
                new Title(problemSeed.Title),
                new Question(problemSeed.Question),
                new Difficulty(problemSeed.Difficulty),
                new TimeLimit(1000),
                new MemoryLimit(64)
            );

            problem.Publish();

            foreach ((Guid versionId, string code, string functionName) in desiredSetups)
                problem.AddSetup(versionId, code, functionName, pipelineId);

            context.Problems.Add(problem);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return problem.Id;
        }

        Guid problemId = existingId.Value;

        HashSet<Guid> existingVersionIds = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .Where(setup => EF.Property<Guid>(setup, "problem_id") == problemId)
            .Select(setup => setup.LanguageVersionId)
            .ToHashSetAsync(cancellationToken);

        List<(Guid versionId, string code, string functionName)> missingSetups = desiredSetups
            .Where(template => !existingVersionIds.Contains(template.versionId))
            .ToList();

        foreach ((Guid versionId, string code, string functionName) in missingSetups)
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO problem_setups (id, problem_id, language_version_id, initial_code, function_name, pipeline_id)
                VALUES ({Guid.NewGuid()}, {problemId}, {versionId}, {code}, {functionName}, {pipelineId})
                """,
                cancellationToken
            );
        }

        return problemId;
    }

    private async Task EnsureTestSuitesLinkedAsync(
        Guid problemId,
        ProblemSeedDefinition problemSeed,
        CancellationToken cancellationToken
    )
    {
        TestSuite[] suites =
        [
            BuildSuite($"{problemSeed.Title} - Sample Cases", TestSuiteType.Sample, problemSeed.SampleCases),
            BuildSuite($"{problemSeed.Title} - Hidden Cases", TestSuiteType.Hidden, problemSeed.HiddenCases),
        ];

        foreach (TestSuite suite in suites)
        {
            TestSuite? existing = await context
                .TestSuites.AsNoTracking()
                .FirstOrDefaultAsync(testSuite => testSuite.Name == suite.Name, cancellationToken);

            Guid suiteId;
            if (existing is null)
            {
                context.TestSuites.Add(suite);
                await context.SaveChangesAsync(cancellationToken);
                context.ChangeTracker.Clear();
                suiteId = suite.Id;
            }
            else
            {
                suiteId = existing.Id;
            }

            List<Guid> setupIds = await context
                .Set<ProblemSetup>()
                .AsNoTracking()
                .Where(setup => EF.Property<Guid>(setup, "problem_id") == problemId)
                .Select(setup => setup.Id)
                .ToListAsync(cancellationToken);

            foreach (Guid setupId in setupIds)
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    INSERT INTO problem_setup_test_suites (problem_setup_id, test_suite_id)
                    VALUES ({setupId}, {suiteId})
                    ON CONFLICT DO NOTHING
                    """,
                    cancellationToken
                );
            }
        }
    }

    private async Task EnsureTagsLinkedAsync(
        Guid problemId,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken
    )
    {
        foreach (string tagName in tags)
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO tags (id, name)
                VALUES ({Guid.NewGuid()}, {tagName})
                ON CONFLICT (name) DO NOTHING
                """,
                cancellationToken
            );

            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO problem_tags ("ProblemsId", "TagsId")
                SELECT {problemId}, id FROM tags WHERE name = {tagName}
                ON CONFLICT DO NOTHING
                """,
                cancellationToken
            );
        }
    }

    private static TestSuite BuildSuite(string name, TestSuiteType suiteType, IReadOnlyCollection<TestCaseSeed> cases)
    {
        TestSuite suite = new(name, suiteType);

        foreach (TestCaseSeed testCaseSeed in cases)
        {
            TestCase testCase = suite.AddTestCase(testCaseSeed.Name);

            foreach (TestCaseValue input in testCaseSeed.Inputs)
                testCase.AddInput(input.Value, input.ValueType);

            foreach (TestCaseValue expectedOutput in testCaseSeed.ExpectedOutputs)
                testCase.AddExpectedOutput(expectedOutput.Value, expectedOutput.ValueType);
        }

        return suite;
    }

    private async Task<LanguageVersionEntry> GetVersionAsync(string slug, CancellationToken cancellationToken) =>
        await context
            .Languages.Where(language => language.Slug == new LanguageSlug(slug))
            .SelectMany(language => language.Versions)
            .OrderBy(version => version.Version)
            .FirstAsync(cancellationToken);

    private sealed record SetupSeed(string LanguageSlug, string Code, string FunctionName);

    private sealed record TestCaseValue(string Value, string ValueType);

    private sealed record TestCaseSeed(
        string Name,
        IReadOnlyCollection<TestCaseValue> Inputs,
        IReadOnlyCollection<TestCaseValue> ExpectedOutputs
    );

    private sealed record TestCaseGenerationSeed(
        string ReferenceSolutionCode,
        IReadOnlyList<GenerationParameterSpec> Parameters,
        string OutputValueType,
        int TargetCaseCount,
        int Seed
    );

    private sealed record ProblemSeedDefinition(
        string Slug,
        string Title,
        string Question,
        int Difficulty,
        IReadOnlyCollection<string> Tags,
        IReadOnlyCollection<SetupSeed> Setups,
        IReadOnlyCollection<TestCaseSeed> SampleCases,
        IReadOnlyCollection<TestCaseSeed> HiddenCases,
        TestCaseGenerationSeed? Generation = null
    );
}