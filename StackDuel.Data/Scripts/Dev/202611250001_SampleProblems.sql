-- Four problems, enough to exercise every track and both Judge0 paths: three general-purpose
-- (Python + JavaScript, which is what required_problem_languages asks for), one of those also
-- with Java so the multi-file path runs, and one SQL problem bound to the Chinook bundle.
-- Hidden cases are generated from the reference solution rather than seeded.
--
-- Published problems need a version to be publishable: Problem.IsPublished checks
-- published_version_id, and CanSubmitForValidation refuses a Published problem that has none.
-- So each problem gets a problem_versions row, a published_version_id, and a snapshot of every
-- setup, exactly as Problem.Publish would have produced.

INSERT INTO tags (id, name) VALUES
    ('3f1b6a10-0000-4000-8000-000000000001', 'arrays'),
    ('3f1b6a10-0000-4000-8000-000000000002', 'hash-table'),
    ('3f1b6a10-0000-4000-8000-000000000003', 'strings'),
    ('3f1b6a10-0000-4000-8000-000000000004', 'greedy'),
    ('3f1b6a10-0000-4000-8000-000000000005', 'sql')
ON CONFLICT DO NOTHING;

INSERT INTO problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, track_id, created_at, published_version_id) VALUES
    (
        '7a1d0001-0000-4000-8000-000000000001',
        'two-sum',
        'Two Sum',
        'Given an array of integers and a target, return the indices of the two numbers that add up to the target. Exactly one pair always sums to the target.',
        150, 1000, 256, 2,
        'c4ca3593-673d-4440-94ed-a59410479c4c',
        NOW(),
        '9e110001-0000-4000-8000-000000000001'
    ),
    (
        '7a1d0001-0000-4000-8000-000000000002',
        'reverse-words',
        'Reverse Words',
        'Given a sentence, return it with the order of the words reversed. Collapse any run of whitespace down to a single space and trim the ends.',
        300, 1000, 256, 2,
        'c4ca3593-673d-4440-94ed-a59410479c4c',
        NOW(),
        '9e110001-0000-4000-8000-000000000002'
    ),
    (
        '7a1d0001-0000-4000-8000-000000000003',
        'minimum-coins-for-change',
        'Minimum Coins for Change',
        'A machine stocks coins worth 1, 5, 10, and 25 cents and always dispenses the fewest coins possible. Return the minimum number of coins that sum to the given amount.',
        500, 1000, 64, 2,
        'c4ca3593-673d-4440-94ed-a59410479c4c',
        NOW(),
        '9e110001-0000-4000-8000-000000000003'
    ),
    (
        '7a1d0001-0000-4000-8000-000000000004',
        'top-genres-by-track-count',
        'Top Genres by Track Count',
        'Using the Chinook database, return the five genres with the most tracks: the genre name and its track count, ordered by count descending and then by name.',
        400, 2000, 256, 2,
        'b33287e4-153d-4f8a-a3e3-8cbfbcea1454',
        NOW(),
        '9e110001-0000-4000-8000-000000000004'
    )
ON CONFLICT DO NOTHING;

INSERT INTO problem_tags ("ProblemsId", "TagsId") VALUES
    ('7a1d0001-0000-4000-8000-000000000001', '3f1b6a10-0000-4000-8000-000000000001'),
    ('7a1d0001-0000-4000-8000-000000000001', '3f1b6a10-0000-4000-8000-000000000002'),
    ('7a1d0001-0000-4000-8000-000000000002', '3f1b6a10-0000-4000-8000-000000000003'),
    ('7a1d0001-0000-4000-8000-000000000003', '3f1b6a10-0000-4000-8000-000000000004'),
    ('7a1d0001-0000-4000-8000-000000000004', '3f1b6a10-0000-4000-8000-000000000005')
ON CONFLICT DO NOTHING;

INSERT INTO problem_setups (id, language_version_id, pipeline_id, initial_code, function_name, reference_solution_code, additional_file_bundle_id, additional_files, problem_id) VALUES
    (
        '5e2d0001-0000-4000-8000-000000000001',
        '5081b28c-a96b-448f-baec-d5bfdd8bcc85',
        '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58',
        E'def two_sum(nums: list[int], target: int) -> list[int]:\n    pass',
        'two_sum',
        E'def two_sum(nums: list[int], target: int) -> list[int]:\n    seen = {}\n    for i, n in enumerate(nums):\n        if target - n in seen:\n            return [seen[target - n], i]\n        seen[n] = i\n    raise ValueError("no solution")',
        NULL, '[]',
        '7a1d0001-0000-4000-8000-000000000001'
    ),
    (
        '5e2d0001-0000-4000-8000-000000000002',
        '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa',
        '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58',
        E'function twoSum(nums, target) {\n    \n}',
        'twoSum',
        E'function twoSum(nums, target) {\n  const seen = new Map();\n  for (let i = 0; i < nums.length; i++) {\n    if (seen.has(target - nums[i])) return [seen.get(target - nums[i]), i];\n    seen.set(nums[i], i);\n  }\n  throw new Error("no solution");\n}',
        NULL, '[]',
        '7a1d0001-0000-4000-8000-000000000001'
    ),
    (
        '5e2d0001-0000-4000-8000-000000000003',
        '5081b28c-a96b-448f-baec-d5bfdd8bcc85',
        '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58',
        E'def reverse_words(sentence: str) -> str:\n    pass',
        'reverse_words',
        E'def reverse_words(sentence: str) -> str:\n    return " ".join(reversed(sentence.split()))',
        NULL, '[]',
        '7a1d0001-0000-4000-8000-000000000002'
    ),
    (
        '5e2d0001-0000-4000-8000-000000000007',
        '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa',
        '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58',
        E'function reverseWords(sentence) {\n    \n}',
        'reverseWords',
        E'function reverseWords(sentence) {\n  return sentence.trim().split(/\\s+/).reverse().join(" ");\n}',
        NULL, '[]',
        '7a1d0001-0000-4000-8000-000000000002'
    ),
    (
        '5e2d0001-0000-4000-8000-000000000004',
        '5081b28c-a96b-448f-baec-d5bfdd8bcc85',
        '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58',
        E'def min_coins(amount: int) -> int:\n    pass',
        'min_coins',
        E'def min_coins(amount: int) -> int:\n    count = 0\n    for coin in (25, 10, 5, 1):\n        count += amount // coin\n        amount %= coin\n    return count',
        NULL, '[]',
        '7a1d0001-0000-4000-8000-000000000003'
    ),
    (
        '5e2d0001-0000-4000-8000-000000000008',
        '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa',
        '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58',
        E'function minCoins(amount) {\n    \n}',
        'minCoins',
        E'function minCoins(amount) {\n  let count = 0;\n  for (const coin of [25, 10, 5, 1]) {\n    count += Math.floor(amount / coin);\n    amount %= coin;\n  }\n  return count;\n}',
        NULL, '[]',
        '7a1d0001-0000-4000-8000-000000000003'
    ),
    (
        '5e2d0001-0000-4000-8000-000000000005',
        '00380d11-5da4-4cff-b2f7-8d0dc0f5acab',
        '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58',
        E'class Solution {\n    public int minCoins(int amount) {\n        return 0;\n    }\n}',
        'minCoins',
        E'class Solution {\n    public int minCoins(int amount) {\n        int count = 0;\n        for (int coin : new int[] { 25, 10, 5, 1 }) {\n            count += amount / coin;\n            amount %= coin;\n        }\n        return count;\n    }\n}',
        NULL, '[]',
        '7a1d0001-0000-4000-8000-000000000003'
    ),
    (
        '5e2d0001-0000-4000-8000-000000000006',
        'a3091d0c-a87b-4317-9a29-4c8a0e5ee62f',
        '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58',
        E'-- Return the five genres with the most tracks\nSELECT',
        NULL,
        E'SELECT g.Name, COUNT(t.TrackId) AS tracks\nFROM genres g\nJOIN tracks t ON t.GenreId = g.GenreId\nGROUP BY g.Name\nORDER BY tracks DESC, g.Name\nLIMIT 5;',
        '18ce3b58-23ab-487d-bae4-dd885fe7f88a', '[]',
        '7a1d0001-0000-4000-8000-000000000004'
    )
ON CONFLICT DO NOTHING;

INSERT INTO problem_versions (id, problem_id, version_number, state, title, question, difficulty, time_limit, memory_limit, published_at, superseded_at)
SELECT v.version_id, p.id, 1, 1, p.title, p.question, p.difficulty, p.time_limit, p.memory_limit, p.created_at, NULL
FROM problems p
JOIN (VALUES
        ('7a1d0001-0000-4000-8000-000000000001'::uuid, '9e110001-0000-4000-8000-000000000001'::uuid),
        ('7a1d0001-0000-4000-8000-000000000002'::uuid, '9e110001-0000-4000-8000-000000000002'::uuid),
        ('7a1d0001-0000-4000-8000-000000000003'::uuid, '9e110001-0000-4000-8000-000000000003'::uuid),
        ('7a1d0001-0000-4000-8000-000000000004'::uuid, '9e110001-0000-4000-8000-000000000004'::uuid)
     ) AS v(problem_id, version_id) ON v.problem_id = p.id
ON CONFLICT DO NOTHING;

INSERT INTO problem_version_setups (id, problem_setup_id, problem_version_id, language_version_id, initial_code, function_name)
SELECT gen_random_uuid(), s.id, p.published_version_id, s.language_version_id, s.initial_code, s.function_name
FROM problem_setups s
JOIN problems p ON p.id = s.problem_id
WHERE p.published_version_id IS NOT NULL
  AND s.id IN (
    '5e2d0001-0000-4000-8000-000000000001',
    '5e2d0001-0000-4000-8000-000000000002',
    '5e2d0001-0000-4000-8000-000000000003',
    '5e2d0001-0000-4000-8000-000000000004',
    '5e2d0001-0000-4000-8000-000000000005',
    '5e2d0001-0000-4000-8000-000000000006',
    '5e2d0001-0000-4000-8000-000000000007',
    '5e2d0001-0000-4000-8000-000000000008'
  )
  AND NOT EXISTS (
        SELECT 1 FROM problem_version_setups existing
        WHERE existing.problem_setup_id = s.id
          AND existing.problem_version_id = p.published_version_id
  );

INSERT INTO test_suites (id, name, type, created_at) VALUES
    ('a5c70001-0000-4000-8000-000000000001', 'Two Sum - Samples', 1, NOW()),
    ('a5c70001-0000-4000-8000-000000000002', 'Reverse Words - Samples', 1, NOW()),
    ('a5c70001-0000-4000-8000-000000000003', 'Minimum Coins - Samples', 1, NOW()),
    ('a5c70001-0000-4000-8000-000000000004', 'Top Genres - Samples', 1, NOW())
ON CONFLICT DO NOTHING;

INSERT INTO problem_setup_test_suites (problem_setup_id, test_suite_id) VALUES
    ('5e2d0001-0000-4000-8000-000000000001', 'a5c70001-0000-4000-8000-000000000001'),
    ('5e2d0001-0000-4000-8000-000000000002', 'a5c70001-0000-4000-8000-000000000001'),
    ('5e2d0001-0000-4000-8000-000000000003', 'a5c70001-0000-4000-8000-000000000002'),
    ('5e2d0001-0000-4000-8000-000000000007', 'a5c70001-0000-4000-8000-000000000002'),
    ('5e2d0001-0000-4000-8000-000000000004', 'a5c70001-0000-4000-8000-000000000003'),
    ('5e2d0001-0000-4000-8000-000000000008', 'a5c70001-0000-4000-8000-000000000003'),
    ('5e2d0001-0000-4000-8000-000000000005', 'a5c70001-0000-4000-8000-000000000003'),
    ('5e2d0001-0000-4000-8000-000000000006', 'a5c70001-0000-4000-8000-000000000004')
ON CONFLICT DO NOTHING;

INSERT INTO test_cases (id, name, source, test_suite_id) VALUES
    ('c0de0001-0000-4000-8000-000000000001', 'Example 1', 0, 'a5c70001-0000-4000-8000-000000000001'),
    ('c0de0001-0000-4000-8000-000000000002', 'Example 2', 0, 'a5c70001-0000-4000-8000-000000000001'),
    ('c0de0001-0000-4000-8000-000000000003', 'Three words', 0, 'a5c70001-0000-4000-8000-000000000002'),
    ('c0de0001-0000-4000-8000-000000000004', 'Extra whitespace', 0, 'a5c70001-0000-4000-8000-000000000002'),
    ('c0de0001-0000-4000-8000-000000000005', 'Quarter plus nickel', 0, 'a5c70001-0000-4000-8000-000000000003'),
    ('c0de0001-0000-4000-8000-000000000006', 'No change owed', 0, 'a5c70001-0000-4000-8000-000000000003'),
    ('c0de0001-0000-4000-8000-000000000007', 'Top five genres', 0, 'a5c70001-0000-4000-8000-000000000004')
ON CONFLICT DO NOTHING;

-- The SQL case has no inputs on purpose: SQLiteCodeTemplateStrategy pastes Inputs[0] in as setup
-- SQL ahead of the submitted query, and this problem needs no setup beyond the Chinook bundle.
INSERT INTO test_case_inputs (id, value, value_type, position, test_case_id) VALUES
    ('1a1a0001-0000-4000-8000-000000000001', '[2,7,11,15]', 'integer_array', 0, 'c0de0001-0000-4000-8000-000000000001'),
    ('1a1a0001-0000-4000-8000-000000000002', '9', 'integer', 1, 'c0de0001-0000-4000-8000-000000000001'),
    ('1a1a0001-0000-4000-8000-000000000003', '[3,2,4]', 'integer_array', 0, 'c0de0001-0000-4000-8000-000000000002'),
    ('1a1a0001-0000-4000-8000-000000000004', '6', 'integer', 1, 'c0de0001-0000-4000-8000-000000000002'),
    ('1a1a0001-0000-4000-8000-000000000005', 'the sky is blue', 'string', 0, 'c0de0001-0000-4000-8000-000000000003'),
    ('1a1a0001-0000-4000-8000-000000000006', '  hello   world  ', 'string', 0, 'c0de0001-0000-4000-8000-000000000004'),
    ('1a1a0001-0000-4000-8000-000000000007', '30', 'integer', 0, 'c0de0001-0000-4000-8000-000000000005'),
    ('1a1a0001-0000-4000-8000-000000000008', '0', 'integer', 0, 'c0de0001-0000-4000-8000-000000000006')
ON CONFLICT DO NOTHING;

-- The SQL expectation is the whole result set as SQLite emits it under `.mode list` with a comma
-- separator and headers off, which is what SQLiteCodeTemplateStrategy.ParseOutput returns.
INSERT INTO test_case_expected_outputs (id, value, value_type, test_case_id) VALUES
    ('0b0b0001-0000-4000-8000-000000000001', '[0,1]', 'integer_array', 'c0de0001-0000-4000-8000-000000000001'),
    ('0b0b0001-0000-4000-8000-000000000002', '[1,2]', 'integer_array', 'c0de0001-0000-4000-8000-000000000002'),
    ('0b0b0001-0000-4000-8000-000000000003', 'blue is sky the', 'string', 'c0de0001-0000-4000-8000-000000000003'),
    ('0b0b0001-0000-4000-8000-000000000004', 'world hello', 'string', 'c0de0001-0000-4000-8000-000000000004'),
    ('0b0b0001-0000-4000-8000-000000000005', '2', 'integer', 'c0de0001-0000-4000-8000-000000000005'),
    ('0b0b0001-0000-4000-8000-000000000006', '0', 'integer', 'c0de0001-0000-4000-8000-000000000006'),
    (
        '0b0b0001-0000-4000-8000-000000000007',
        E'Rock,1297\nLatin,579\nMetal,374\nAlternative & Punk,332\nJazz,130',
        'string',
        'c0de0001-0000-4000-8000-000000000007'
    )
ON CONFLICT DO NOTHING;
