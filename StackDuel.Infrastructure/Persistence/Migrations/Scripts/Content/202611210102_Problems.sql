INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('b84aded7-a976-4702-be1d-357dab06b5c7', 'two-sum', 'Two Sum', 'Given an array of integers `nums` and an integer `target`, return indices of the two numbers such that they add up to `target`.

You may assume that each input would have exactly one solution, and you may not use the same element twice. You can return the answer in any order.

### Example 1

```
Input: nums = [2,7,11,15], target = 9
Output: [0,1]
Explanation: nums[0] + nums[1] = 2 + 7 = 9, so the answer is [0, 1].
```

### Example 2

```
Input: nums = [3,2,4], target = 6
Output: [1,2]
Explanation: nums[1] + nums[2] = 2 + 4 = 6, so the answer is [1, 2].
```

### Constraints

1. `2 <= nums.length <= 10^4`
2. `-10^9 <= nums[i] <= 10^9`
3. `-10^9 <= target <= 10^9`
4. Exactly one valid pair exists.', 500, 1000, 64, 2, '2026-11-21 20:31:34.135384+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('82d4098e-a541-4fb3-9407-6268db4dd747', 'hello-or-goodbye', 'Hello or Goodbye', 'Given an integer `n`, return `"Hello"` if `n` is even, or `"Goodbye"` if `n` is odd.

### Example 1

```
Input: n = 4
Output: "Hello"
Explanation: 4 is even, so the answer is "Hello".
```

### Example 2

```
Input: n = 7
Output: "Goodbye"
Explanation: 7 is odd, so the answer is "Goodbye".
```

### Example 3

```
Input: n = 0
Output: "Hello"
Explanation: 0 is even, so the answer is "Hello".
```

### Constraints

1. `-10^9 <= n <= 10^9`', 100, 1000, 64, 2, '2026-11-21 20:31:34.421564+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('8a928089-2e45-4ab7-9c7b-aa95e7567a33', 'sum-two-integers', 'Sum Two Integers', 'Given two integers a and b, return the sum a + b.

Example 1:
Input: a = 2, b = 3
Output: 5

Example 2:
Input: a = -4, b = 10
Output: 6

Constraints:
-1000 <= a, b <= 1000', 50, 1000, 64, 2, '2026-11-21 20:31:34.51189+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('453600f3-a03f-4fe6-a258-82f847a7ad95', 'is-even', 'Is Even', 'Given an integer n, return true if n is even, otherwise return false.

Example 1:
Input: n = 8
Output: true

Example 2:
Input: n = 7
Output: false

Constraints:
-10^9 <= n <= 10^9', 140, 1000, 64, 2, '2026-11-21 20:31:34.574439+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('affe99cd-a7a7-4815-873a-a7c1b8c4d71c', 'reverse-word', 'Reverse Word', 'Given a non-empty string word, return a new string with the characters in reverse order.

Example 1:
Input: word = "code"
Output: "edoc"

Example 2:
Input: word = "racecar"
Output: "racecar"

Constraints:
1 <= word.length <= 100
word contains only lowercase English letters.', 260, 1000, 64, 2, '2026-11-21 20:31:34.620415+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('1454b026-f7a4-45fc-a161-11f562d7b3be', 'max-of-three', 'Max of Three', 'Given three integers a, b, and c, return the largest value among them.

Example 1:
Input: a = 1, b = 7, c = 3
Output: 7

Example 2:
Input: a = -5, b = -2, c = -9
Output: -2

Constraints:
-10^6 <= a, b, c <= 10^6', 420, 1000, 64, 2, '2026-11-21 20:31:34.671925+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('19a2c325-6158-45e1-8a56-585b37e37b45', 'count-vowels', 'Count Vowels', 'Given a string s consisting of lowercase English letters, return the number of vowels (a, e, i, o, u) in s.

Example 1:
Input: s = "hello"
Output: 2

Example 2:
Input: s = "rhythm"
Output: 0

Constraints:
1 <= s.length <= 100
s consists of lowercase English letters only.', 180, 1000, 64, 2, '2026-11-21 20:31:34.718395+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('c43e62cc-b6c6-4a49-97e5-05868f80d163', 'array-sum', 'Array Sum', 'Given an array of integers nums, return the sum of all elements.

Example 1:
Input: nums = [1,2,3]
Output: 6

Example 2:
Input: nums = [-4,5,10]
Output: 11

Constraints:
1 <= nums.length <= 100
-1000 <= nums[i] <= 1000', 150, 1000, 64, 2, '2026-11-21 20:31:34.783724+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('23cbcaf1-45f9-4d56-84d9-e1d45725139d', 'find-max-in-array', 'Find Maximum In Array', 'Given a non-empty array of integers nums, return the largest value in the array.

Example 1:
Input: nums = [3,1,4,1,5]
Output: 5

Example 2:
Input: nums = [-8,-2,-9]
Output: -2

Constraints:
1 <= nums.length <= 100
-1000 <= nums[i] <= 1000', 230, 1000, 64, 2, '2026-11-21 20:31:34.831654+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('7d20e5b9-f8c8-48b1-b876-d5e8e147de8d', 'palindrome-number', 'Palindrome Number', 'Given an integer n, return true if n is a palindrome (reads the same forwards and backwards), otherwise return false. Negative numbers are never palindromes.

Example 1:
Input: n = 121
Output: true

Example 2:
Input: n = -121
Output: false

Constraints:
-1000000 <= n <= 1000000', 320, 1000, 64, 2, '2026-11-21 20:31:34.878789+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('cbe07da4-8ed6-461b-8a11-91674f3847e7', 'second-largest', 'Second Largest', 'Given an array of integers nums containing at least two distinct values, return the second largest distinct value.

Example 1:
Input: nums = [4,2,9,9,5]
Output: 5

Example 2:
Input: nums = [1,1,2]
Output: 1

Constraints:
2 <= nums.length <= 100
-1000 <= nums[i] <= 1000
nums contains at least two distinct values.', 700, 1000, 64, 2, '2026-11-21 20:31:34.925251+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('29a901b4-087b-42ad-afab-4fff76149f74', 'multiply-two-integers', 'Multiply Two Integers', 'Given two integers a and b, return the product a * b.

Example 1:
Input: a = 3, b = 4
Output: 12

Example 2:
Input: a = -2, b = 5
Output: -10

Constraints:
-1000 <= a, b <= 1000', 60, 1000, 64, 2, '2026-11-21 20:31:34.975736+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('5f3afd0f-e24e-4dca-ada1-5bcdcfd69fb9', 'is-positive', 'Is Positive', 'Given an integer n, return true if n is strictly greater than 0, otherwise return false.

Example 1:
Input: n = 5
Output: true

Example 2:
Input: n = -3
Output: false

Constraints:
-10^9 <= n <= 10^9', 40, 1000, 64, 2, '2026-11-21 20:31:35.031344+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('0dd93b36-51b4-4416-96f8-9270b19df5c0', 'count-consonants', 'Count Consonants', 'Given a string s consisting of lowercase English letters, return the number of consonants (every letter except a, e, i, o, u) in s.

Example 1:
Input: s = "hello"
Output: 3

Example 2:
Input: s = "aeiou"
Output: 0

Constraints:
1 <= s.length <= 100
s consists of lowercase English letters only.', 190, 1000, 64, 2, '2026-11-21 20:31:35.087375+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('b261c4b6-6cb8-4cbb-a9ea-94940a470555', 'average-of-array', 'Average Of Array', 'Given a non-empty array of integers nums, return the average of all elements as a floating-point number.

Example 1:
Input: nums = [1,2,3]
Output: 2.0

Example 2:
Input: nums = [4,8,15,16]
Output: 10.75

Constraints:
1 <= nums.length <= 100
-1000 <= nums[i] <= 1000', 260, 1000, 64, 2, '2026-11-21 20:31:35.133039+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('5c4fa5df-98ff-4721-b624-0104223e275f', 'find-min-in-array', 'Find Minimum In Array', 'Given a non-empty array of integers nums, return the smallest value in the array.

Example 1:
Input: nums = [3,1,4,1,5]
Output: 1

Example 2:
Input: nums = [-8,-2,-9]
Output: -9

Constraints:
1 <= nums.length <= 100
-1000 <= nums[i] <= 1000', 235, 1000, 64, 2, '2026-11-21 20:31:35.182384+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('33b14dff-f06a-4a28-8a0b-84b2ef73cfba', 'is-prime', 'Is Prime', 'Given a positive integer n, return true if n is a prime number, otherwise return false.

Example 1:
Input: n = 7
Output: true

Example 2:
Input: n = 8
Output: false

Constraints:
1 <= n <= 1000000', 560, 1000, 64, 2, '2026-11-21 20:31:35.223856+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('3e18d91c-72b0-4b6f-a24c-c1325580e5ac', 'fibonacci-number', 'Fibonacci Number', 'Given an integer n, return the nth Fibonacci number, where fib(0) = 0, fib(1) = 1, and fib(n) = fib(n-1) + fib(n-2) for n > 1.

Example 1:
Input: n = 5
Output: 5

Example 2:
Input: n = 10
Output: 55

Constraints:
0 <= n <= 40', 610, 1000, 64, 2, '2026-11-21 20:31:35.269229+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('b175e193-c960-4248-97de-1cc3004b71a7', 'string-length', 'String Length', 'Given a string s, return the number of characters in s.

Example 1:
Input: s = "hello"
Output: 5

Example 2:
Input: s = "zoo"
Output: 3

Constraints:
1 <= s.length <= 1000', 20, 1000, 64, 2, '2026-11-21 20:31:35.314694+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('44d3058c-dc51-4515-9953-3f7bd4843369', 'contains-duplicate', 'Contains Duplicate', 'Given an array of integers nums, return true if any value appears at least twice in the array, and return false if every element is distinct.

Example 1:
Input: nums = [1,2,3,1]
Output: true

Example 2:
Input: nums = [1,2,3,4]
Output: false

Constraints:
1 <= nums.length <= 100
-1000 <= nums[i] <= 1000', 350, 1000, 64, 2, '2026-11-21 20:31:35.355661+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('acd4883b-906c-4561-84f8-83f9dff9a5ea', 'capitalize-first-letter', 'Capitalize First Letter', 'Given a non-empty string word consisting of lowercase English letters, return a new string with the first letter capitalized and the rest unchanged.

Example 1:
Input: word = "apple"
Output: "Apple"

Example 2:
Input: word = "zoo"
Output: "Zoo"

Constraints:
1 <= word.length <= 100
word consists of lowercase English letters only.', 210, 1000, 64, 2, '2026-11-21 20:31:35.402386+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('b4c15e3b-55c4-4f86-b9df-0aa2b8b127e7', 'absolute-difference', 'Absolute Difference', 'Given two integers a and b, return the absolute value of their difference.

Example 1:
Input: a = 10, b = 3
Output: 7

Example 2:
Input: a = -5, b = 5
Output: 10

Constraints:
-1000 <= a, b <= 1000', 65, 1000, 64, 2, '2026-11-21 20:31:35.446597+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('22e16147-592e-4c37-91f2-fd9ca3b18f18', 'celsius-to-fahrenheit', 'Celsius To Fahrenheit', 'Given a temperature celsius in degrees Celsius, return the equivalent temperature in degrees Fahrenheit using the formula F = (C * 9/5) + 32.

Example 1:
Input: celsius = 0
Output: 32.0

Example 2:
Input: celsius = 100
Output: 212.0

Constraints:
-273 <= celsius <= 1000', 85, 1000, 64, 2, '2026-11-21 20:31:35.497955+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('799f873c-3311-4f9a-9def-46bae5790cd0', 'factorial', 'Factorial', 'Given a non-negative integer n, return n! (n factorial), the product of all positive integers less than or equal to n. By definition, 0! = 1.

Example 1:
Input: n = 5
Output: 120

Example 2:
Input: n = 0
Output: 1

Constraints:
0 <= n <= 12', 240, 1000, 64, 2, '2026-11-21 20:31:35.543441+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('dd53b18e-efcc-4ab8-9056-4924f284416d', 'is-leap-year', 'Is Leap Year', 'Given an integer year, return true if year is a leap year in the Gregorian calendar, otherwise return false. A year is a leap year if it is divisible by 4, except for years divisible by 100 unless also divisible by 400.

Example 1:
Input: year = 2024
Output: true

Example 2:
Input: year = 1900
Output: false

Constraints:
1 <= year <= 9999', 150, 1000, 64, 2, '2026-11-21 20:31:35.603352+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('bd99d921-de82-4165-9abc-c67780ccc55b', 'digit-sum', 'Digit Sum', 'Given a non-negative integer n, return the sum of its digits.

Example 1:
Input: n = 1234
Output: 10

Example 2:
Input: n = 7
Output: 7

Constraints:
0 <= n <= 1000000000', 175, 1000, 64, 2, '2026-11-21 20:31:35.657957+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('0dc2d857-4aea-4d70-9994-a94eab8664f7', 'count-occurrences', 'Count Occurrences', 'Given an array of integers nums and an integer target, return the number of times target appears in nums.

Example 1:
Input: nums = [1,2,2,3,2], target = 2
Output: 3

Example 2:
Input: nums = [5,5,5], target = 1
Output: 0

Constraints:
1 <= nums.length <= 100
-1000 <= nums[i], target <= 1000', 165, 1000, 64, 2, '2026-11-21 20:31:35.702168+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('5515726d-68ab-4fa6-a77a-4a905e4bbaf4', 'sum-of-even-numbers', 'Sum Of Even Numbers', 'Given an array of integers nums, return the sum of all even numbers in the array.

Example 1:
Input: nums = [1,2,3,4,5,6]
Output: 12

Example 2:
Input: nums = [1,3,5]
Output: 0

Constraints:
1 <= nums.length <= 100
-1000 <= nums[i] <= 1000', 195, 1000, 64, 2, '2026-11-21 20:31:35.743832+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('3d67cf3f-fd75-4560-a70f-3ae08a486aac', 'power-of-two', 'Power Of Two', 'Given an integer n, return true if n is a power of two (that is, n = 2^k for some non-negative integer k), otherwise return false.

Example 1:
Input: n = 16
Output: true

Example 2:
Input: n = 18
Output: false

Constraints:
-1000000 <= n <= 1000000', 250, 1000, 64, 2, '2026-11-21 20:31:35.793849+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('a4dd5244-e40f-487b-93b3-b8ce9ca1e072', 'gcd-of-two-numbers', 'GCD Of Two Numbers', 'Given two positive integers a and b, return their greatest common divisor (GCD).

Example 1:
Input: a = 12, b = 18
Output: 6

Example 2:
Input: a = 17, b = 5
Output: 1

Constraints:
1 <= a, b <= 1000000', 330, 1000, 64, 2, '2026-11-21 20:31:35.841291+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('9d6ee1c9-14e2-41e4-be45-b662f3fae252', 'valid-anagram', 'Valid Anagram', 'Given two strings s and t, return true if t is an anagram of s (the same characters with the same frequencies, in any order), otherwise return false.

Example 1:
Input: s = "listen", t = "silent"
Output: true

Example 2:
Input: s = "rat", t = "car"
Output: false

Constraints:
1 <= s.length, t.length <= 30
s and t consist of lowercase English letters.', 390, 1000, 64, 2, '2026-11-21 20:31:35.885982+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('c054a21f-f9fc-4122-90c1-7df55d079ec2', 'sum-of-numbers-sql', 'Sum of Numbers', 'Write a query that returns the sum of all values in the `numbers` table.

| Column | Type    | Description         |
|--------|---------|----------------------|
| value  | INTEGER | One number per row   |

### Example 1

```
numbers table:
 value
-------
     1
     2
     3

Output: 6
```

### Example 2

```
numbers table:
 value
-------
    10
    -4
     5

Output: 11
```', 150, 1000, 64, 2, '2026-11-21 20:31:35.950789+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('14f33027-470f-4793-9f21-61cf85d0ae28', 'artists-starting-with-a-sql', 'Artists Starting With A', 'Write a query that returns the names of all artists whose name starts with ''A'', ordered by `ArtistId`.

The `Artists` table:

| Column   | Type    | Description             |
|----------|---------|--------------------------|
| ArtistId | INTEGER | Primary key              |
| Name     | TEXT    | Artist name (nullable)   |', 200, 1000, 64, 2, '2026-11-21 20:31:36.062046+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('1600278c-3f34-4d52-be90-846c0e15730c', 'count-all-artists-sql', 'Count All Artists', 'Write a query that returns the total number of artists in the table.

The `Artists` table:

| Column   | Type    | Description             |
|----------|---------|--------------------------|
| ArtistId | INTEGER | Primary key              |
| Name     | TEXT    | Artist name (nullable)   |', 100, 1000, 64, 2, '2026-11-21 20:31:36.084781+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('9bba250b-364f-457f-8be2-c1ab090278dc', 'click-counter', 'Click Counter', 'Implement a React component named `Counter` that receives a `start` prop and renders a single `<button>` showing the current count.

### Requirements

- The button must have the attribute `data-testid="count"`.
- The button''s text content is the current count, beginning at `start`.
- Each click on the button increments the count by 1.

### Example 1

```
Input: start = 0
Render: <button data-testid="count">0</button>
```

### Example 2

```
Input: start = 5
After one click: <button data-testid="count">6</button>
```', 200, 2000, 64, 3, '2026-11-21 20:31:36.113587+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('ae3d8793-87f6-49d3-bfc4-7cdd7be87758', 'vanilla-click-counter', 'Click Counter (Vanilla JS)', 'Implement a function named `render` that receives a `container` DOM element and a `props` object with a `start` field, and appends a single `<button>` showing the current count.

### Requirements

- Build the button with `document.createElement` and append it to `container`.
- The button must have the attribute `data-testid="count"`.
- The button''s text content is the current count, beginning at `start`.
- Each click on the button increments the count by 1.

### Example 1

```
Input: start = 0
Render: <button data-testid="count">0</button>
```

### Example 2

```
Input: start = 5
After one click: <button data-testid="count">6</button>
```', 200, 2000, 64, 1, '2026-11-21 20:31:36.158123+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;
INSERT INTO public.problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at, created_by_id, validation_failure_reason, track_id) VALUES ('95b2585d-f65c-4df2-ba73-e469d4ab88c9', 'artist-discography', 'Artist Discography', 'Implement a React component named `ArtistList` that receives an `artists` prop — an array of `{ id, name, records }` objects, where `records` is an array of `{ id, title, year }` objects — and renders each artist with their records collapsed by default, expandable independently on click.

### Requirements

- Render a `<div data-testid="artist-list">` wrapping one entry per artist.
- Each artist entry needs a `<button data-testid="toggle-{artist.id}">` showing the artist''s name, which toggles that artist''s records visibility on click.
- Records must start collapsed (not rendered at all) and only appear once toggled.
- When expanded, render `<ul data-testid="records-{artist.id}">` containing one `<li data-testid="record-{record.id}">` per record with the text `{title} ({year})`.
- Each artist''s expanded/collapsed state is independent of the others — expanding one must not affect any other artist.
- An artist with zero records still renders an (empty) `<ul>` once expanded.

### Example 1

```
Input: artists = [{ id: "a1", name: "Radiohead", records: [{ id: "r1", title: "OK Computer", year: 1997 }] }]
Render (collapsed): <button data-testid="toggle-a1">Radiohead</button>
```

### Example 2

```
Input: artists = [{ id: "a1", name: "Radiohead", records: [{ id: "r1", title: "OK Computer", year: 1997 }] }]
After clicking "Radiohead": adds <ul data-testid="records-a1"><li data-testid="record-r1">OK Computer (1997)</li></ul>
```', 600, 2000, 64, 3, '2026-11-21 20:31:36.200968+00', NULL, NULL, NULL) ON CONFLICT DO NOTHING;



INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('2a025f1d-b107-4a06-9683-66be31baa54e', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    vector<int> twoSum(vector<int>& nums, int target) {
        return {};
    }
};', 'twoSum', 'b84aded7-a976-4702-be1d-357dab06b5c7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('3c31dd7d-4599-478c-b4de-54fb7cb78d34', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function twoSum(nums, target) {
    
}', 'twoSum', 'b84aded7-a976-4702-be1d-357dab06b5c7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('9d7bce8d-a34e-4e7d-a208-93e6dd4847fa', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int[] twoSum(int[] nums, int target) {
        return null;
    }
}', 'twoSum', 'b84aded7-a976-4702-be1d-357dab06b5c7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('baf4af7f-1077-4eb6-b5a6-7d4ef5a7bb0c', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def two_sum(nums: list[int], target: int) -> list[int]:
    pass', 'two_sum', 'b84aded7-a976-4702-be1d-357dab06b5c7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('13f10248-26c3-47b4-a0f9-5e64298e3f6c', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function helloOrGoodbye(n) {
    
}', 'helloOrGoodbye', '82d4098e-a541-4fb3-9407-6268db4dd747', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5f8546bb-2d05-4ee6-b9e9-e5ed8316a1e1', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public String helloOrGoodbye(int n) {
        return null;
    }
}', 'helloOrGoodbye', '82d4098e-a541-4fb3-9407-6268db4dd747', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('7aee1875-7cba-4b6b-b364-888ad9ccf615', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    string helloOrGoodbye(int n) {
        return "";
    }
};', 'helloOrGoodbye', '82d4098e-a541-4fb3-9407-6268db4dd747', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('ad14b1d1-d08a-4331-9bb3-b0fafcc74843', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def hello_or_goodbye(n: int) -> str:
    pass', 'hello_or_goodbye', '82d4098e-a541-4fb3-9407-6268db4dd747', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('337c31c6-c748-463e-8085-5976c4a23789', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int sumTwoIntegers(int a, int b) {
        return 0;
    }
}', 'sumTwoIntegers', '8a928089-2e45-4ab7-9c7b-aa95e7567a33', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('549b0e90-1188-4904-b6bc-85e6da5f70fb', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def sum_two_integers(a: int, b: int) -> int:
    pass', 'sum_two_integers', '8a928089-2e45-4ab7-9c7b-aa95e7567a33', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('8211f77c-450c-4806-91c6-6e1e977cc81f', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int sumTwoIntegers(int a, int b) {
        return 0;
    }
};', 'sumTwoIntegers', '8a928089-2e45-4ab7-9c7b-aa95e7567a33', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('b0b830d3-7db8-479b-9d94-21e76741310f', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function sumTwoIntegers(a, b) {
    
}', 'sumTwoIntegers', '8a928089-2e45-4ab7-9c7b-aa95e7567a33', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('356a4de6-625a-4942-8ec1-aeef23e3e00c', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def is_even(n: int) -> bool:
    pass', 'is_even', '453600f3-a03f-4fe6-a258-82f847a7ad95', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('4d17df11-c36d-4613-8b00-8677bc15d437', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public boolean isEven(int n) {
        return false;
    }
}', 'isEven', '453600f3-a03f-4fe6-a258-82f847a7ad95', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5b278194-d884-42c0-9447-d9f3010241bc', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    bool isEven(int n) {
        return false;
    }
};', 'isEven', '453600f3-a03f-4fe6-a258-82f847a7ad95', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('6596232e-614b-4e1f-bd91-09763ee380c9', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function isEven(n) {
    
}', 'isEven', '453600f3-a03f-4fe6-a258-82f847a7ad95', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('53efb8c4-ab8b-4e49-9ba1-23b519778e5b', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def reverse_word(word: str) -> str:
    pass', 'reverse_word', 'affe99cd-a7a7-4815-873a-a7c1b8c4d71c', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5dd00c6f-4238-4ccc-bb03-a5f8959945d0', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public String reverseWord(String word) {
        return null;
    }
}', 'reverseWord', 'affe99cd-a7a7-4815-873a-a7c1b8c4d71c', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('8646ee19-04c1-47bc-a150-892889a5bc8c', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function reverseWord(word) {
    
}', 'reverseWord', 'affe99cd-a7a7-4815-873a-a7c1b8c4d71c', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('deed6422-1078-4860-8d65-94baaf839902', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    string reverseWord(string word) {
        return "";
    }
};', 'reverseWord', 'affe99cd-a7a7-4815-873a-a7c1b8c4d71c', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('05028735-68e9-469c-b869-634d652f80dd', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def max_of_three(a: int, b: int, c: int) -> int:
    pass', 'max_of_three', '1454b026-f7a4-45fc-a161-11f562d7b3be', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('6e02ec6e-3783-49f5-aea8-ba5ec7e14a06', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int maxOfThree(int a, int b, int c) {
        return 0;
    }
};', 'maxOfThree', '1454b026-f7a4-45fc-a161-11f562d7b3be', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('8817b1e3-ad79-4213-9bdd-3fd3bcc880e4', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int maxOfThree(int a, int b, int c) {
        return 0;
    }
}', 'maxOfThree', '1454b026-f7a4-45fc-a161-11f562d7b3be', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('923f948f-ecf8-44b5-a74b-ca01b07bd6b2', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function maxOfThree(a, b, c) {
    
}', 'maxOfThree', '1454b026-f7a4-45fc-a161-11f562d7b3be', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c07fbb98-d023-4e56-8ea7-db2ce074c2f5', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function countVowels(s) {
    
}', 'countVowels', '19a2c325-6158-45e1-8a56-585b37e37b45', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c4035f65-a88e-4a2a-801b-e086921498e9', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int countVowels(string s) {
        return 0;
    }
};', 'countVowels', '19a2c325-6158-45e1-8a56-585b37e37b45', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('da8d0954-eb7a-4fb0-a2dd-e5be7ecd18e8', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int countVowels(String s) {
        return 0;
    }
}', 'countVowels', '19a2c325-6158-45e1-8a56-585b37e37b45', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('e5d9aff5-55ab-41d3-a3c1-0e01fbff4e6f', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def count_vowels(s: str) -> int:
    pass', 'count_vowels', '19a2c325-6158-45e1-8a56-585b37e37b45', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('4d632b36-2e87-47ac-a464-858fe4668408', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function arraySum(nums) {
    
}', 'arraySum', 'c43e62cc-b6c6-4a49-97e5-05868f80d163', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('da1ec150-bfc4-49e5-8688-4ca110e0e005', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int arraySum(int[] nums) {
        return 0;
    }
}', 'arraySum', 'c43e62cc-b6c6-4a49-97e5-05868f80d163', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('df88f0c6-3d26-44fb-86ca-efe289329e9b', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int arraySum(vector<int>& nums) {
        return 0;
    }
};', 'arraySum', 'c43e62cc-b6c6-4a49-97e5-05868f80d163', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('fc2b870e-db61-4c3a-89e1-aac4866488d6', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def array_sum(nums: list[int]) -> int:
    pass', 'array_sum', 'c43e62cc-b6c6-4a49-97e5-05868f80d163', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5c944e73-7cf5-4e54-b21f-5c238ac94f07', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int findMax(int[] nums) {
        return 0;
    }
}', 'findMax', '23cbcaf1-45f9-4d56-84d9-e1d45725139d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('aa2c242e-005a-48ee-96eb-5ea97e2ec12f', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def find_max(nums: list[int]) -> int:
    pass', 'find_max', '23cbcaf1-45f9-4d56-84d9-e1d45725139d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c2c73cee-406e-4715-8061-cf1b94bc9a7e', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function findMax(nums) {
    
}', 'findMax', '23cbcaf1-45f9-4d56-84d9-e1d45725139d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('fdc252af-7cd7-4b0a-b6f4-0bcbb5c1dce3', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int findMax(vector<int>& nums) {
        return 0;
    }
};', 'findMax', '23cbcaf1-45f9-4d56-84d9-e1d45725139d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('05f8e4e1-b288-4854-be68-a65c6c00a794', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    bool isPalindromeNumber(int n) {
        return false;
    }
};', 'isPalindromeNumber', '7d20e5b9-f8c8-48b1-b876-d5e8e147de8d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5e8657ac-ed06-4cb9-a30d-913c1063a850', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function isPalindromeNumber(n) {
    
}', 'isPalindromeNumber', '7d20e5b9-f8c8-48b1-b876-d5e8e147de8d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5fc2e7c2-77aa-452d-892c-2266d630d434', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def is_palindrome_number(n: int) -> bool:
    pass', 'is_palindrome_number', '7d20e5b9-f8c8-48b1-b876-d5e8e147de8d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('656b444d-f927-4d82-9621-3a4a53da7ed4', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public boolean isPalindromeNumber(int n) {
        return false;
    }
}', 'isPalindromeNumber', '7d20e5b9-f8c8-48b1-b876-d5e8e147de8d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('6a123617-6af1-4883-9bbe-582d7284cd0d', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function secondLargest(nums) {
    
}', 'secondLargest', 'cbe07da4-8ed6-461b-8a11-91674f3847e7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('8ec52756-a9e0-4201-a774-b942b335c878', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int secondLargest(int[] nums) {
        return 0;
    }
}', 'secondLargest', 'cbe07da4-8ed6-461b-8a11-91674f3847e7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('9b4c2d5e-6eae-4a7b-aa11-4e1f8d9d38d1', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def second_largest(nums: list[int]) -> int:
    pass', 'second_largest', 'cbe07da4-8ed6-461b-8a11-91674f3847e7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c6b13ea0-8a12-4ffe-ad96-4c894bc116be', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int secondLargest(vector<int>& nums) {
        return 0;
    }
};', 'secondLargest', 'cbe07da4-8ed6-461b-8a11-91674f3847e7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('16c4092c-8152-4e76-87ef-f7350e8217c4', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def multiply_two_integers(a: int, b: int) -> int:
    pass', 'multiply_two_integers', '29a901b4-087b-42ad-afab-4fff76149f74', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('24e9a295-a58d-4650-890b-1ce3e3314ff7', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int multiplyTwoIntegers(int a, int b) {
        return 0;
    }
};', 'multiplyTwoIntegers', '29a901b4-087b-42ad-afab-4fff76149f74', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('64521841-a964-4a09-8759-281b610eecc8', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int multiplyTwoIntegers(int a, int b) {
        return 0;
    }
}', 'multiplyTwoIntegers', '29a901b4-087b-42ad-afab-4fff76149f74', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c55dc81f-6bf5-4446-b539-158d3a4f8c72', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function multiplyTwoIntegers(a, b) {
    
}', 'multiplyTwoIntegers', '29a901b4-087b-42ad-afab-4fff76149f74', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('532f4873-da26-47af-8c88-98e629436f21', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function isPositive(n) {
    
}', 'isPositive', '5f3afd0f-e24e-4dca-ada1-5bcdcfd69fb9', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('70ba5739-0d8e-4b9f-bad0-bdce18f3d521', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public boolean isPositive(int n) {
        return false;
    }
}', 'isPositive', '5f3afd0f-e24e-4dca-ada1-5bcdcfd69fb9', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c6491372-facb-4995-b5c3-0724d7489d9b', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def is_positive(n: int) -> bool:
    pass', 'is_positive', '5f3afd0f-e24e-4dca-ada1-5bcdcfd69fb9', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d569b52f-38e3-471e-9ef4-9e46baa60873', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    bool isPositive(int n) {
        return false;
    }
};', 'isPositive', '5f3afd0f-e24e-4dca-ada1-5bcdcfd69fb9', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5546de9b-f6db-4c4f-99d5-4178632e2ebd', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int countConsonants(string s) {
        return 0;
    }
};', 'countConsonants', '0dd93b36-51b4-4416-96f8-9270b19df5c0', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('6b265b72-a492-4168-925e-7fa8f487d75f', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int countConsonants(String s) {
        return 0;
    }
}', 'countConsonants', '0dd93b36-51b4-4416-96f8-9270b19df5c0', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('743f8220-f1d6-45aa-86bd-5278e2df041a', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function countConsonants(s) {
    
}', 'countConsonants', '0dd93b36-51b4-4416-96f8-9270b19df5c0', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('b8c117b9-0a00-4ba5-871f-4e1fa74fd9f7', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def count_consonants(s: str) -> int:
    pass', 'count_consonants', '0dd93b36-51b4-4416-96f8-9270b19df5c0', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('00c6fc9f-bd17-42fa-a1dd-e4ac47125072', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function averageOfArray(nums) {
    
}', 'averageOfArray', 'b261c4b6-6cb8-4cbb-a9ea-94940a470555', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5b1feec2-18dd-4bd5-a5d0-940acb70433c', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    double averageOfArray(vector<int>& nums) {
        return 0;
    }
};', 'averageOfArray', 'b261c4b6-6cb8-4cbb-a9ea-94940a470555', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c20e221d-df33-450b-b90f-b0f35a5aa75c', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public double averageOfArray(int[] nums) {
        return 0;
    }
}', 'averageOfArray', 'b261c4b6-6cb8-4cbb-a9ea-94940a470555', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d80ee8d5-c461-438d-96cb-aae18fa4b621', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def average_of_array(nums: list[int]) -> float:
    pass', 'average_of_array', 'b261c4b6-6cb8-4cbb-a9ea-94940a470555', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('32a523e1-ac9b-4105-9e8d-8edad95b5e1d', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function findMin(nums) {
    
}', 'findMin', '5c4fa5df-98ff-4721-b624-0104223e275f', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('6ee2a208-478f-4ffe-b994-c8436d23be57', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int findMin(int[] nums) {
        return 0;
    }
}', 'findMin', '5c4fa5df-98ff-4721-b624-0104223e275f', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('dccf8bea-e46b-49d5-8a7a-e2832157cb4d', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def find_min(nums: list[int]) -> int:
    pass', 'find_min', '5c4fa5df-98ff-4721-b624-0104223e275f', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('ea9980fa-b0e1-4160-ba7a-6de6bf4d74e2', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int findMin(vector<int>& nums) {
        return 0;
    }
};', 'findMin', '5c4fa5df-98ff-4721-b624-0104223e275f', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('0ce91d50-0896-4e96-ab31-1a26018906ed', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def is_prime(n: int) -> bool:
    pass', 'is_prime', '33b14dff-f06a-4a28-8a0b-84b2ef73cfba', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('4c4f45ec-c80f-4bf1-9ff4-9bb668694297', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    bool isPrime(int n) {
        return false;
    }
};', 'isPrime', '33b14dff-f06a-4a28-8a0b-84b2ef73cfba', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('84128551-4b58-45fb-8866-3346615e4d7e', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public boolean isPrime(int n) {
        return false;
    }
}', 'isPrime', '33b14dff-f06a-4a28-8a0b-84b2ef73cfba', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('921be321-730b-4101-b496-fc2ad1c4137a', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function isPrime(n) {
    
}', 'isPrime', '33b14dff-f06a-4a28-8a0b-84b2ef73cfba', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('aa69d7a6-7fd9-449b-a4f7-ec277a65693e', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int fibonacciNumber(int n) {
        return 0;
    }
}', 'fibonacciNumber', '3e18d91c-72b0-4b6f-a24c-c1325580e5ac', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('ca3e7362-006d-436b-b3b9-5f038b56ab16', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function fibonacciNumber(n) {
    
}', 'fibonacciNumber', '3e18d91c-72b0-4b6f-a24c-c1325580e5ac', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d19b2ec9-f14e-4b23-a1d5-b76369017be6', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int fibonacciNumber(int n) {
        return 0;
    }
};', 'fibonacciNumber', '3e18d91c-72b0-4b6f-a24c-c1325580e5ac', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d837bd2e-5ba6-4d6b-b06f-af70a3aca82d', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def fibonacci_number(n: int) -> int:
    pass', 'fibonacci_number', '3e18d91c-72b0-4b6f-a24c-c1325580e5ac', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5cff703c-b01b-479a-a56f-f8773b1684ee', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def string_length(s: str) -> int:
    pass', 'string_length', 'b175e193-c960-4248-97de-1cc3004b71a7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d379aa7c-e985-4f74-b7fd-61b6efaab35c', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int stringLength(string s) {
        return 0;
    }
};', 'stringLength', 'b175e193-c960-4248-97de-1cc3004b71a7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d4784439-5712-4118-86ea-983a0153da94', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int stringLength(String s) {
        return 0;
    }
}', 'stringLength', 'b175e193-c960-4248-97de-1cc3004b71a7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('efd58771-ab14-4cab-8962-24e18e32603d', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function stringLength(s) {
    
}', 'stringLength', 'b175e193-c960-4248-97de-1cc3004b71a7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('b789cec6-ed13-4085-8fae-dfc3ff2871b8', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public boolean containsDuplicate(int[] nums) {
        return false;
    }
}', 'containsDuplicate', '44d3058c-dc51-4515-9953-3f7bd4843369', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('bf7d31f1-6512-49ac-bde1-b8b88cddeec8', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def contains_duplicate(nums: list[int]) -> bool:
    pass', 'contains_duplicate', '44d3058c-dc51-4515-9953-3f7bd4843369', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c147f4bf-e1f8-444d-bf3c-9afee004a631', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    bool containsDuplicate(vector<int>& nums) {
        return false;
    }
};', 'containsDuplicate', '44d3058c-dc51-4515-9953-3f7bd4843369', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d7d61e21-e93d-4650-968b-8be7dbddbd28', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function containsDuplicate(nums) {
    
}', 'containsDuplicate', '44d3058c-dc51-4515-9953-3f7bd4843369', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('057be62f-f5e0-4562-a2c7-6b695c407e33', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def capitalize_first_letter(word: str) -> str:
    pass', 'capitalize_first_letter', 'acd4883b-906c-4561-84f8-83f9dff9a5ea', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('133a77b2-53f1-473a-adaf-11dad49d43ee', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    string capitalizeFirstLetter(string word) {
        return "";
    }
};', 'capitalizeFirstLetter', 'acd4883b-906c-4561-84f8-83f9dff9a5ea', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('6751d380-41d1-4a7c-8629-6d885ff040d7', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public String capitalizeFirstLetter(String word) {
        return null;
    }
}', 'capitalizeFirstLetter', 'acd4883b-906c-4561-84f8-83f9dff9a5ea', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('9c548b8e-5dea-4d43-a783-ae6001de52e6', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function capitalizeFirstLetter(word) {
    
}', 'capitalizeFirstLetter', 'acd4883b-906c-4561-84f8-83f9dff9a5ea', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('2b2be864-73f8-4ae9-8b4f-5ae4500b1bc8', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def absolute_difference(a: int, b: int) -> int:
    pass', 'absolute_difference', 'b4c15e3b-55c4-4f86-b9df-0aa2b8b127e7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('3cd208f9-b4b2-44e8-a388-c9860546c523', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int absoluteDifference(int a, int b) {
        return 0;
    }
}', 'absoluteDifference', 'b4c15e3b-55c4-4f86-b9df-0aa2b8b127e7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('781ea09d-3397-47cc-bf94-30732b4baf1e', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function absoluteDifference(a, b) {
    
}', 'absoluteDifference', 'b4c15e3b-55c4-4f86-b9df-0aa2b8b127e7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('940155dc-031a-49af-a659-e3ccab42fd97', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int absoluteDifference(int a, int b) {
        return 0;
    }
};', 'absoluteDifference', 'b4c15e3b-55c4-4f86-b9df-0aa2b8b127e7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('1e248c6c-8a99-4682-a6bc-a97892e639dc', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public double celsiusToFahrenheit(double celsius) {
        return 0;
    }
}', 'celsiusToFahrenheit', '22e16147-592e-4c37-91f2-fd9ca3b18f18', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('4b4a9a93-ebe6-49f5-a807-58dfddd46568', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function celsiusToFahrenheit(celsius) {
    
}', 'celsiusToFahrenheit', '22e16147-592e-4c37-91f2-fd9ca3b18f18', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('6888d457-fcdf-4e32-9732-ea10aa8d56d9', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def celsius_to_fahrenheit(celsius: float) -> float:
    pass', 'celsius_to_fahrenheit', '22e16147-592e-4c37-91f2-fd9ca3b18f18', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('e77e7cba-a699-40b0-87e6-8b20fb1a4722', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    double celsiusToFahrenheit(double celsius) {
        return 0;
    }
};', 'celsiusToFahrenheit', '22e16147-592e-4c37-91f2-fd9ca3b18f18', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('3fc19c11-dad4-4a29-86f7-c04757c9de87', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int factorial(int n) {
        return 0;
    }
};', 'factorial', '799f873c-3311-4f9a-9def-46bae5790cd0', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('a88489cd-979b-449e-82f1-911d9e5e874f', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def factorial(n: int) -> int:
    pass', 'factorial', '799f873c-3311-4f9a-9def-46bae5790cd0', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('c4df1259-c0af-4032-872d-0f55ac575a76', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function factorial(n) {
    
}', 'factorial', '799f873c-3311-4f9a-9def-46bae5790cd0', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('fc2118f8-0de3-4d39-81d1-606145cb20c4', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int factorial(int n) {
        return 0;
    }
}', 'factorial', '799f873c-3311-4f9a-9def-46bae5790cd0', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5c64f6ea-011c-4dcf-83d0-4f5533a05b28', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public boolean isLeapYear(int year) {
        return false;
    }
}', 'isLeapYear', 'dd53b18e-efcc-4ab8-9056-4924f284416d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('98c58af8-e5e4-40d6-93d1-4c080f3a4ff9', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    bool isLeapYear(int year) {
        return false;
    }
};', 'isLeapYear', 'dd53b18e-efcc-4ab8-9056-4924f284416d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d2c2bce9-81a8-4452-bcd9-5b0a8ddf7d43', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function isLeapYear(year) {
    
}', 'isLeapYear', 'dd53b18e-efcc-4ab8-9056-4924f284416d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d9163fc6-cbd8-4b2e-ba95-32d0100b6415', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def is_leap_year(year: int) -> bool:
    pass', 'is_leap_year', 'dd53b18e-efcc-4ab8-9056-4924f284416d', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('12c6c05b-3655-4834-9f90-78070b45f511', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int digitSum(int n) {
        return 0;
    }
};', 'digitSum', 'bd99d921-de82-4165-9abc-c67780ccc55b', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('44c6b728-5a13-4b3c-807c-a254f7f80b73', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def digit_sum(n: int) -> int:
    pass', 'digit_sum', 'bd99d921-de82-4165-9abc-c67780ccc55b', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('78b51565-1d24-4d44-8f6f-fa2f7e3e83be', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int digitSum(int n) {
        return 0;
    }
}', 'digitSum', 'bd99d921-de82-4165-9abc-c67780ccc55b', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('e70c06bd-2e43-4259-a824-dbf5e751d739', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function digitSum(n) {
    
}', 'digitSum', 'bd99d921-de82-4165-9abc-c67780ccc55b', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('1342fcd5-b722-41f7-9fab-939593466fc0', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def count_occurrences(nums: list[int], target: int) -> int:
    pass', 'count_occurrences', '0dc2d857-4aea-4d70-9994-a94eab8664f7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('1c44914f-c45f-40c0-8936-908fd3b8f267', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int countOccurrences(int[] nums, int target) {
        return 0;
    }
}', 'countOccurrences', '0dc2d857-4aea-4d70-9994-a94eab8664f7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('53734abd-9a3e-45af-ba38-5ecfbf1861ee', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int countOccurrences(vector<int>& nums, int target) {
        return 0;
    }
};', 'countOccurrences', '0dc2d857-4aea-4d70-9994-a94eab8664f7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('9f1e5803-b8f5-4e75-95f3-fb3067e31ae1', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function countOccurrences(nums, target) {
    
}', 'countOccurrences', '0dc2d857-4aea-4d70-9994-a94eab8664f7', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('0170d86a-e0e3-4cd4-8c84-d17596e0015c', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int sumOfEvenNumbers(vector<int>& nums) {
        return 0;
    }
};', 'sumOfEvenNumbers', '5515726d-68ab-4fa6-a77a-4a905e4bbaf4', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('3733e9e5-837d-4d93-936a-3591277645c1', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function sumOfEvenNumbers(nums) {
    
}', 'sumOfEvenNumbers', '5515726d-68ab-4fa6-a77a-4a905e4bbaf4', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('7f81fed4-8460-471c-92b9-25001c3c095b', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def sum_of_even_numbers(nums: list[int]) -> int:
    pass', 'sum_of_even_numbers', '5515726d-68ab-4fa6-a77a-4a905e4bbaf4', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d0998e7c-5700-48d4-a25b-afaa9462c18f', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int sumOfEvenNumbers(int[] nums) {
        return 0;
    }
}', 'sumOfEvenNumbers', '5515726d-68ab-4fa6-a77a-4a905e4bbaf4', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('11af0a95-b4a2-4180-a8cb-0734da8107ad', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def is_power_of_two(n: int) -> bool:
    pass', 'is_power_of_two', '3d67cf3f-fd75-4560-a70f-3ae08a486aac', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('35c07da4-6ac4-427a-a2e6-52f27c31f474', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    bool isPowerOfTwo(int n) {
        return false;
    }
};', 'isPowerOfTwo', '3d67cf3f-fd75-4560-a70f-3ae08a486aac', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('391fc802-6eb7-47d8-b6a0-b13ee8c69846', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public boolean isPowerOfTwo(int n) {
        return false;
    }
}', 'isPowerOfTwo', '3d67cf3f-fd75-4560-a70f-3ae08a486aac', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d0d7b796-0f92-4f97-a88d-a07964731d3a', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function isPowerOfTwo(n) {
    
}', 'isPowerOfTwo', '3d67cf3f-fd75-4560-a70f-3ae08a486aac', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('1b81ba2c-273e-46a8-8eb0-d99b425869fe', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function gcdOfTwoNumbers(a, b) {
    
}', 'gcdOfTwoNumbers', 'a4dd5244-e40f-487b-93b3-b8ce9ca1e072', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('3bd1f19e-de30-41bc-ad7b-980b4c36573c', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    int gcdOfTwoNumbers(int a, int b) {
        return 0;
    }
};', 'gcdOfTwoNumbers', 'a4dd5244-e40f-487b-93b3-b8ce9ca1e072', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('83d9d5b7-57d2-48ac-ad5f-0487d3cc2b67', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def gcd_of_two_numbers(a: int, b: int) -> int:
    pass', 'gcd_of_two_numbers', 'a4dd5244-e40f-487b-93b3-b8ce9ca1e072', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('88f6456b-0786-4d18-80c2-0c3f20db3c7b', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public int gcdOfTwoNumbers(int a, int b) {
        return 0;
    }
}', 'gcdOfTwoNumbers', 'a4dd5244-e40f-487b-93b3-b8ce9ca1e072', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('020732a8-61a1-4779-8683-e7ce7bc90ccf', 'eeb2be35-a342-4573-8007-880fa4f97ba2', 'class Solution {
    public boolean isAnagram(String s, String t) {
        return false;
    }
}', 'isAnagram', '9d6ee1c9-14e2-41e4-be45-b662f3fae252', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('042480a7-9411-4360-959c-0a75f3c05a6a', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 'function isAnagram(s, t) {
    
}', 'isAnagram', '9d6ee1c9-14e2-41e4-be45-b662f3fae252', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('63dc5be6-8bc1-42e7-9b25-ae73e8917478', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 'def is_anagram(s: str, t: str) -> bool:
    pass', 'is_anagram', '9d6ee1c9-14e2-41e4-be45-b662f3fae252', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('904cb6fa-6390-42a9-bbf1-d9ee5b57250a', 'c6d6c86f-7c03-4503-bd55-4877d6165b0e', 'class Solution {
public:
    bool isAnagram(string s, string t) {
        return false;
    }
};', 'isAnagram', '9d6ee1c9-14e2-41e4-be45-b662f3fae252', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('06ea2e1c-6c17-4e39-a527-657cef256cf2', 'a3091d0c-a87b-4317-9a29-4c8a0e5ee62f', '-- Write your SQLite query statement below
', NULL, 'c054a21f-f9fc-4122-90c1-7df55d079ec2', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('d12db58d-1602-4ccc-b46c-311295b345c4', 'a3091d0c-a87b-4317-9a29-4c8a0e5ee62f', '-- Write your SQLite query statement below
', NULL, '14f33027-470f-4793-9f21-61cf85d0ae28', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, '18ce3b58-23ab-487d-bae4-dd885fe7f88a', '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('403daa00-eefc-4222-8107-f4e1596a5ac1', 'a3091d0c-a87b-4317-9a29-4c8a0e5ee62f', '-- Write your SQLite query statement below
', NULL, '1600278c-3f34-4d52-be90-846c0e15730c', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, '18ce3b58-23ab-487d-bae4-dd885fe7f88a', '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('5c1dcf96-0f19-445e-976c-5aa1a08e859d', '00380d11-5da4-4cff-b2f7-8d0dc0f5acab', 'function Counter({ start }) {
  return null;
}', 'Counter', '9bba250b-364f-457f-8be2-c1ab090278dc', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('a2776298-077a-45c7-a370-7928442d6263', '95a7bae2-0233-404d-9a8d-be2d876f4df0', 'function render(container, { start }) {

}', 'render', 'ae3d8793-87f6-49d3-bfc4-7cdd7be87758', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[]') ON CONFLICT DO NOTHING;
INSERT INTO public.problem_setups (id, language_version_id, initial_code, function_name, problem_id, pipeline_id, generation_spec_id, reference_solution_code, additional_file_bundle_id, additional_files) VALUES ('f0012ad6-6c84-4ed4-be14-6e8837ade0ab', '00380d11-5da4-4cff-b2f7-8d0dc0f5acab', 'const { RecordItem } = require("./RecordItem");

function ArtistList({ artists }) {
  return null;
}', 'ArtistList', '95b2585d-f65c-4df2-ba73-e469d4ab88c9', '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', NULL, NULL, NULL, '[{"Path":"RecordItem.jsx","Content":"function RecordItem({ record }) {\n  return (\n    \u003Cli data-testid={\u0060record-${record.id}\u0060}\u003E\n      {record.title} ({record.year})\n    \u003C/li\u003E\n  );\n}\n\nmodule.exports.RecordItem = RecordItem;"}]') ON CONFLICT DO NOTHING;
