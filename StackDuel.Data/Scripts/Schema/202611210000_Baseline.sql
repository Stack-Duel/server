CREATE TABLE achievement_definitions (
    id uuid NOT NULL,
    code character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    description character varying(1000) NOT NULL,
    category integer NOT NULL,
    tier integer NOT NULL,
    icon_key character varying(100) NOT NULL,
    is_secret boolean NOT NULL,
    is_active boolean NOT NULL,
    criteria_type integer NOT NULL,
    criteria_stat integer,
    criteria_threshold integer,
    custom_rule_key character varying(100),
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_achievement_definitions" PRIMARY KEY (id)
);


CREATE TABLE additional_file_bundles (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    content bytea NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_additional_file_bundles" PRIMARY KEY (id)
);


CREATE TABLE audit_log_entries (
    id uuid NOT NULL,
    actor_user_id uuid,
    actor_username character varying(256) NOT NULL,
    action character varying(128) NOT NULL,
    target_type character varying(64),
    target_id character varying(128),
    details_json jsonb,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_audit_log_entries" PRIMARY KEY (id)
);


CREATE TABLE campaigns (
    id uuid NOT NULL,
    slug character varying(200) NOT NULL,
    title character varying(200) NOT NULL,
    description character varying(4000) NOT NULL,
    difficulty integer NOT NULL,
    status integer NOT NULL,
    icon_key character varying(200),
    sort_order integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    published_at timestamp with time zone,
    CONSTRAINT "PK_campaigns" PRIMARY KEY (id)
);


CREATE TABLE execution_pipelines (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    description text,
    CONSTRAINT "PK_execution_pipelines" PRIMARY KEY (id)
);


CREATE TABLE feature_flags (
    id uuid NOT NULL,
    key character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    description character varying(1000) NOT NULL,
    default_enabled boolean NOT NULL,
    rollout_percentage integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_feature_flags" PRIMARY KEY (id)
);


CREATE TABLE groups (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    CONSTRAINT "PK_groups" PRIMARY KEY (id)
);


CREATE TABLE permissions (
    id uuid NOT NULL,
    code text NOT NULL,
    description character varying(500) NOT NULL,
    CONSTRAINT "PK_permissions" PRIMARY KEY (id)
);


CREATE TABLE problem_pools (
    id uuid NOT NULL,
    key character varying(50) NOT NULL,
    name character varying(100) NOT NULL,
    description text,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_problem_pools" PRIMARY KEY (id)
);


CREATE TABLE problem_reaction_types (
    id uuid NOT NULL,
    key character varying(50) NOT NULL,
    name character varying(100) NOT NULL,
    emoji character varying(16),
    sort_order integer NOT NULL,
    is_enabled boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_problem_reaction_types" PRIMARY KEY (id)
);


CREATE TABLE problem_validation_jobs (
    id uuid NOT NULL,
    problem_id uuid NOT NULL,
    status integer NOT NULL,
    test_case_generation_job_ids jsonb NOT NULL,
    result_summary text,
    failure_reason text,
    created_at timestamp with time zone NOT NULL,
    completed_at timestamp with time zone,
    locked_until timestamp with time zone,
    CONSTRAINT "PK_problem_validation_jobs" PRIMARY KEY (id)
);


CREATE TABLE required_problem_languages (
    id uuid NOT NULL,
    language_version_id uuid NOT NULL,
    sort_order integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_required_problem_languages" PRIMARY KEY (id)
);


CREATE TABLE roles (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    CONSTRAINT "PK_roles" PRIMARY KEY (id)
);


CREATE TABLE security_restrictions (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "PermissionCode" text NOT NULL,
    "Effect" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "Reason" text NOT NULL,
    "DetectionEventId" uuid NOT NULL,
    CONSTRAINT "PK_security_restrictions" PRIMARY KEY ("Id")
);


CREATE TABLE tags (
    id uuid NOT NULL,
    name character varying(50) NOT NULL,
    CONSTRAINT "PK_tags" PRIMARY KEY (id)
);


CREATE TABLE test_case_generation_jobs (
    id uuid NOT NULL,
    problem_setup_id uuid NOT NULL,
    reference_solution_code text NOT NULL,
    parameters jsonb NOT NULL,
    output_value_type character varying(100) NOT NULL,
    target_case_count integer NOT NULL,
    seed integer NOT NULL,
    status integer NOT NULL,
    result_summary text,
    failure_reason text,
    created_at timestamp with time zone NOT NULL,
    completed_at timestamp with time zone,
    locked_until timestamp with time zone,
    CONSTRAINT "PK_test_case_generation_jobs" PRIMARY KEY (id)
);


CREATE TABLE test_case_generation_specs (
    id uuid NOT NULL,
    problem_setup_id uuid NOT NULL,
    parameters jsonb NOT NULL,
    output_value_type character varying(100) NOT NULL,
    target_case_count integer NOT NULL,
    seed integer NOT NULL,
    version integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_test_case_generation_specs" PRIMARY KEY (id)
);


CREATE TABLE test_suites (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    type integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_test_suites" PRIMARY KEY (id)
);


CREATE TABLE tracks (
    id uuid NOT NULL,
    key character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    is_active boolean NOT NULL,
    allows_language_selection boolean NOT NULL,
    CONSTRAINT "PK_tracks" PRIMARY KEY (id)
);


CREATE TABLE users (
    id uuid NOT NULL,
    tenant character varying(64),
    bio character varying(500),
    image_url character varying(2048),
    sub character varying(255) NOT NULL,
    username character varying(20) NOT NULL,
    username_last_changed_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    setup_completed_at timestamp with time zone,
    is_private boolean NOT NULL,
    CONSTRAINT "PK_users" PRIMARY KEY (id)
);


CREATE TABLE campaign_modules (
    id uuid NOT NULL,
    "CampaignId" uuid NOT NULL,
    title character varying(200) NOT NULL,
    description character varying(4000) NOT NULL,
    sort_order integer NOT NULL,
    campaign_id uuid NOT NULL,
    CONSTRAINT "PK_campaign_modules" PRIMARY KEY (id),
    CONSTRAINT "FK_campaign_modules_campaigns_campaign_id" FOREIGN KEY (campaign_id) REFERENCES campaigns (id) ON DELETE CASCADE
);


CREATE TABLE campaign_prerequisites (
    id uuid NOT NULL,
    "CampaignId" uuid NOT NULL,
    required_campaign_id uuid NOT NULL,
    campaign_id uuid NOT NULL,
    CONSTRAINT "PK_campaign_prerequisites" PRIMARY KEY (id),
    CONSTRAINT "FK_campaign_prerequisites_campaigns_campaign_id" FOREIGN KEY (campaign_id) REFERENCES campaigns (id) ON DELETE CASCADE,
    CONSTRAINT "FK_campaign_prerequisites_campaigns_required_campaign_id" FOREIGN KEY (required_campaign_id) REFERENCES campaigns (id) ON DELETE RESTRICT
);


CREATE TABLE execution_pipeline_steps (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    step_type integer NOT NULL,
    step_order integer NOT NULL,
    max_attempts integer NOT NULL,
    timeout_seconds integer NOT NULL,
    is_polling boolean NOT NULL,
    pipeline_id uuid NOT NULL,
    CONSTRAINT "PK_execution_pipeline_steps" PRIMARY KEY (id),
    CONSTRAINT "FK_execution_pipeline_steps_execution_pipelines_pipeline_id" FOREIGN KEY (pipeline_id) REFERENCES execution_pipelines (id) ON DELETE CASCADE
);


CREATE TABLE feature_flag_user_overrides (
    user_id uuid NOT NULL,
    feature_flag_id uuid NOT NULL,
    effect text NOT NULL,
    CONSTRAINT "PK_feature_flag_user_overrides" PRIMARY KEY (user_id, feature_flag_id),
    CONSTRAINT "FK_feature_flag_user_overrides_feature_flags_feature_flag_id" FOREIGN KEY (feature_flag_id) REFERENCES feature_flags (id) ON DELETE CASCADE
);


CREATE TABLE feature_flag_group_overrides (
    group_id uuid NOT NULL,
    feature_flag_id uuid NOT NULL,
    effect text NOT NULL,
    CONSTRAINT "PK_feature_flag_group_overrides" PRIMARY KEY (group_id, feature_flag_id),
    CONSTRAINT "FK_feature_flag_group_overrides_feature_flags_feature_flag_id" FOREIGN KEY (feature_flag_id) REFERENCES feature_flags (id) ON DELETE CASCADE,
    CONSTRAINT "FK_feature_flag_group_overrides_groups_group_id" FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE CASCADE
);


CREATE TABLE game_modes (
    id uuid NOT NULL,
    key character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    description character varying(1000) NOT NULL,
    is_built_in boolean NOT NULL,
    is_active boolean NOT NULL,
    min_players integer NOT NULL,
    max_players integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    default_pool_id uuid NOT NULL,
    CONSTRAINT "PK_game_modes" PRIMARY KEY (id),
    CONSTRAINT "FK_game_modes_problem_pools_default_pool_id" FOREIGN KEY (default_pool_id) REFERENCES problem_pools (id) ON DELETE RESTRICT
);


CREATE TABLE group_roles (
    group_id uuid NOT NULL,
    role_id uuid NOT NULL,
    CONSTRAINT "PK_group_roles" PRIMARY KEY (group_id, role_id),
    CONSTRAINT "FK_group_roles_groups_group_id" FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE CASCADE,
    CONSTRAINT "FK_group_roles_roles_role_id" FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE
);


CREATE TABLE role_permissions (
    permission_id uuid NOT NULL,
    role_id uuid NOT NULL,
    effect text NOT NULL,
    CONSTRAINT "PK_role_permissions" PRIMARY KEY (permission_id, role_id),
    CONSTRAINT "FK_role_permissions_permissions_permission_id" FOREIGN KEY (permission_id) REFERENCES permissions (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_role_permissions_roles_role_id" FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE
);


CREATE TABLE test_cases (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    description text,
    source integer NOT NULL DEFAULT 0,
    generation_spec_id uuid,
    generation_case_index integer,
    retired_at timestamp with time zone,
    test_suite_id uuid NOT NULL,
    CONSTRAINT "PK_test_cases" PRIMARY KEY (id),
    CONSTRAINT "FK_test_cases_test_case_generation_specs_generation_spec_id" FOREIGN KEY (generation_spec_id) REFERENCES test_case_generation_specs (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_test_cases_test_suites_test_suite_id" FOREIGN KEY (test_suite_id) REFERENCES test_suites (id) ON DELETE CASCADE
);


CREATE TABLE languages (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    slug character varying(100) NOT NULL,
    track_id uuid NOT NULL,
    status integer NOT NULL,
    CONSTRAINT "PK_languages" PRIMARY KEY (id),
    CONSTRAINT "FK_languages_tracks_track_id" FOREIGN KEY (track_id) REFERENCES tracks (id) ON DELETE RESTRICT
);


CREATE TABLE campaign_enrollments (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    campaign_id uuid NOT NULL,
    status integer NOT NULL,
    enrolled_at timestamp with time zone NOT NULL,
    completed_at timestamp with time zone,
    CONSTRAINT "PK_campaign_enrollments" PRIMARY KEY (id),
    CONSTRAINT "FK_campaign_enrollments_campaigns_campaign_id" FOREIGN KEY (campaign_id) REFERENCES campaigns (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_campaign_enrollments_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);


CREATE TABLE feedback_submissions (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    type integer NOT NULL,
    message character varying(2000) NOT NULL,
    rating integer,
    context_type integer NOT NULL,
    context_entity_id uuid,
    status integer NOT NULL,
    admin_note character varying(2000),
    page_url character varying(2000),
    user_agent character varying(500),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone,
    CONSTRAINT "PK_feedback_submissions" PRIMARY KEY (id),
    CONSTRAINT "FK_feedback_submissions_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);


CREATE TABLE problems (
    id uuid NOT NULL,
    slug character varying(200) NOT NULL,
    title character varying(200) NOT NULL,
    question character varying(10000) NOT NULL,
    difficulty integer NOT NULL,
    time_limit integer NOT NULL,
    memory_limit integer NOT NULL,
    status integer NOT NULL,
    validation_failure_reason character varying(4000),
    track_id uuid,
    created_at timestamp with time zone NOT NULL,
    created_by_id uuid,
    CONSTRAINT "PK_problems" PRIMARY KEY (id),
    CONSTRAINT "FK_problems_tracks_track_id" FOREIGN KEY (track_id) REFERENCES tracks (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_problems_users_created_by_id" FOREIGN KEY (created_by_id) REFERENCES users (id) ON DELETE SET NULL
);


CREATE TABLE user_achievement_stats (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    accepted_solve_count integer NOT NULL DEFAULT 0,
    current_solve_streak integer NOT NULL DEFAULT 0,
    longest_solve_streak integer NOT NULL DEFAULT 0,
    last_solve_date_utc date,
    games_played integer NOT NULL DEFAULT 0,
    games_won integer NOT NULL DEFAULT 0,
    games_lost integer NOT NULL DEFAULT 0,
    games_drawn integer NOT NULL DEFAULT 0,
    current_win_streak integer NOT NULL DEFAULT 0,
    longest_win_streak integer NOT NULL DEFAULT 0,
    bug_reports_submitted integer NOT NULL DEFAULT 0,
    solved_difficulty_mask integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_user_achievement_stats" PRIMARY KEY (id),
    CONSTRAINT "FK_user_achievement_stats_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);


CREATE TABLE user_achievements (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    achievement_definition_id uuid NOT NULL,
    earned_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_user_achievements" PRIMARY KEY (id),
    CONSTRAINT "FK_user_achievements_achievement_definitions_achievement_defin~" FOREIGN KEY (achievement_definition_id) REFERENCES achievement_definitions (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_user_achievements_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);


CREATE TABLE user_avatars (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    image_url character varying(2048) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_user_avatars" PRIMARY KEY (id),
    CONSTRAINT "FK_user_avatars_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);


CREATE TABLE user_groups (
    user_id uuid NOT NULL,
    group_id uuid NOT NULL,
    CONSTRAINT "PK_user_groups" PRIMARY KEY (user_id, group_id),
    CONSTRAINT "FK_user_groups_groups_group_id" FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE CASCADE,
    CONSTRAINT "FK_user_groups_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);


CREATE TABLE campaign_units (
    id uuid NOT NULL,
    "CampaignModuleId" uuid NOT NULL,
    title character varying(200) NOT NULL,
    content character varying(20000) NOT NULL,
    unit_type integer NOT NULL,
    estimated_minutes integer NOT NULL,
    sort_order integer NOT NULL,
    campaign_module_id uuid NOT NULL,
    CONSTRAINT "PK_campaign_units" PRIMARY KEY (id),
    CONSTRAINT "FK_campaign_units_campaign_modules_campaign_module_id" FOREIGN KEY (campaign_module_id) REFERENCES campaign_modules (id) ON DELETE CASCADE
);


CREATE TABLE assert_step_configurations (
    id uuid NOT NULL,
    pipeline_step_id uuid NOT NULL,
    strategy integer NOT NULL,
    tolerance numeric,
    case_sensitive boolean NOT NULL,
    CONSTRAINT "PK_assert_step_configurations" PRIMARY KEY (id),
    CONSTRAINT "FK_assert_step_configurations_execution_pipeline_steps_pipelin~" FOREIGN KEY (pipeline_step_id) REFERENCES execution_pipeline_steps (id) ON DELETE CASCADE
);


CREATE TABLE judge0_step_configurations (
    id uuid NOT NULL,
    pipeline_step_id uuid NOT NULL,
    is_encoded boolean NOT NULL,
    should_wait boolean NOT NULL,
    strip_whitespace boolean NOT NULL,
    default_timeout_seconds integer NOT NULL,
    CONSTRAINT "PK_judge0_step_configurations" PRIMARY KEY (id),
    CONSTRAINT "FK_judge0_step_configurations_execution_pipeline_steps_pipelin~" FOREIGN KEY (pipeline_step_id) REFERENCES execution_pipeline_steps (id) ON DELETE CASCADE
);


CREATE TABLE game_mode_time_options (
    id uuid NOT NULL,
    duration_seconds integer NOT NULL,
    is_default boolean NOT NULL,
    game_mode_id uuid NOT NULL,
    CONSTRAINT "PK_game_mode_time_options" PRIMARY KEY (id),
    CONSTRAINT "FK_game_mode_time_options_game_modes_game_mode_id" FOREIGN KEY (game_mode_id) REFERENCES game_modes (id) ON DELETE CASCADE
);


CREATE TABLE games (
    id uuid NOT NULL,
    lobby_id uuid,
    game_mode_id uuid NOT NULL,
    join_code character varying(7) NOT NULL,
    time_limit_in_seconds integer NOT NULL,
    skips_enabled boolean NOT NULL DEFAULT TRUE,
    status integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    started_at timestamp with time zone,
    ended_at timestamp with time zone,
    "ScheduledExpirySequenceNumber" bigint,
    pool_id uuid NOT NULL,
    CONSTRAINT "PK_games" PRIMARY KEY (id),
    CONSTRAINT "FK_games_game_modes_game_mode_id" FOREIGN KEY (game_mode_id) REFERENCES game_modes (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_games_problem_pools_pool_id" FOREIGN KEY (pool_id) REFERENCES problem_pools (id) ON DELETE RESTRICT
);


CREATE TABLE leaderboards (
    id uuid NOT NULL,
    game_mode_id uuid NOT NULL,
    time_limit_in_seconds integer NOT NULL,
    CONSTRAINT "PK_leaderboards" PRIMARY KEY (id),
    CONSTRAINT "FK_leaderboards_game_modes_game_mode_id" FOREIGN KEY (game_mode_id) REFERENCES game_modes (id) ON DELETE RESTRICT
);


CREATE TABLE test_case_expected_outputs (
    id uuid NOT NULL,
    value text NOT NULL,
    value_type character varying(100) NOT NULL,
    test_case_id uuid NOT NULL,
    CONSTRAINT "PK_test_case_expected_outputs" PRIMARY KEY (id),
    CONSTRAINT "FK_test_case_expected_outputs_test_cases_test_case_id" FOREIGN KEY (test_case_id) REFERENCES test_cases (id) ON DELETE CASCADE
);


CREATE TABLE test_case_inputs (
    id uuid NOT NULL,
    value text NOT NULL,
    value_type character varying(100) NOT NULL,
    position integer NOT NULL,
    test_case_id uuid NOT NULL,
    CONSTRAINT "PK_test_case_inputs" PRIMARY KEY (id),
    CONSTRAINT "FK_test_case_inputs_test_cases_test_case_id" FOREIGN KEY (test_case_id) REFERENCES test_cases (id) ON DELETE CASCADE
);


CREATE TABLE language_versions (
    id uuid NOT NULL,
    judge0_id integer NOT NULL,
    status integer NOT NULL,
    version character varying(50) NOT NULL,
    execution_mode integer NOT NULL DEFAULT 0,
    language_id uuid NOT NULL,
    CONSTRAINT "PK_language_versions" PRIMARY KEY (id),
    CONSTRAINT "FK_language_versions_languages_language_id" FOREIGN KEY (language_id) REFERENCES languages (id) ON DELETE CASCADE
);


CREATE TABLE user_language_preferences (
    user_id uuid NOT NULL,
    language_id uuid NOT NULL,
    position integer NOT NULL,
    CONSTRAINT "PK_user_language_preferences" PRIMARY KEY (user_id, language_id),
    CONSTRAINT "FK_user_language_preferences_languages_language_id" FOREIGN KEY (language_id) REFERENCES languages (id) ON DELETE CASCADE,
    CONSTRAINT "FK_user_language_preferences_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);


CREATE TABLE daily_challenges (
    id uuid NOT NULL,
    challenge_date date NOT NULL,
    problem_id uuid NOT NULL,
    CONSTRAINT "PK_daily_challenges" PRIMARY KEY (id),
    CONSTRAINT "FK_daily_challenges_problems_problem_id" FOREIGN KEY (problem_id) REFERENCES problems (id) ON DELETE RESTRICT
);


CREATE TABLE problem_generation_parameters (
    "ProblemId" uuid NOT NULL,
    seed integer NOT NULL,
    parameters jsonb NOT NULL,
    output_value_type character varying(100) NOT NULL,
    target_case_count integer NOT NULL,
    CONSTRAINT "PK_problem_generation_parameters" PRIMARY KEY ("ProblemId"),
    CONSTRAINT "FK_problem_generation_parameters_problems_ProblemId" FOREIGN KEY ("ProblemId") REFERENCES problems (id) ON DELETE CASCADE
);


CREATE TABLE problem_history (
    id uuid NOT NULL,
    title character varying(200) NOT NULL,
    question character varying(10000) NOT NULL,
    difficulty integer NOT NULL,
    time_limit integer NOT NULL,
    memory_limit integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    problem_id uuid NOT NULL,
    CONSTRAINT "PK_problem_history" PRIMARY KEY (id),
    CONSTRAINT "FK_problem_history_problems_problem_id" FOREIGN KEY (problem_id) REFERENCES problems (id) ON DELETE CASCADE
);


CREATE TABLE problem_pool_problems (
    id uuid NOT NULL,
    problem_id uuid NOT NULL,
    position integer NOT NULL,
    added_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    pool_id uuid NOT NULL,
    CONSTRAINT "PK_problem_pool_problems" PRIMARY KEY (id),
    CONSTRAINT "FK_problem_pool_problems_problem_pools_pool_id" FOREIGN KEY (pool_id) REFERENCES problem_pools (id) ON DELETE CASCADE,
    CONSTRAINT "FK_problem_pool_problems_problems_problem_id" FOREIGN KEY (problem_id) REFERENCES problems (id) ON DELETE CASCADE
);


CREATE TABLE problem_reactions (
    id uuid NOT NULL,
    problem_id uuid NOT NULL,
    user_id uuid NOT NULL,
    reaction_type_id uuid NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_problem_reactions" PRIMARY KEY (id),
    CONSTRAINT "FK_problem_reactions_problem_reaction_types_reaction_type_id" FOREIGN KEY (reaction_type_id) REFERENCES problem_reaction_types (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_problem_reactions_problems_problem_id" FOREIGN KEY (problem_id) REFERENCES problems (id) ON DELETE CASCADE,
    CONSTRAINT "FK_problem_reactions_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);


CREATE TABLE problem_tags (
    "ProblemsId" uuid NOT NULL,
    "TagsId" uuid NOT NULL,
    CONSTRAINT "PK_problem_tags" PRIMARY KEY ("ProblemsId", "TagsId"),
    CONSTRAINT "FK_problem_tags_problems_ProblemsId" FOREIGN KEY ("ProblemsId") REFERENCES problems (id) ON DELETE CASCADE,
    CONSTRAINT "FK_problem_tags_tags_TagsId" FOREIGN KEY ("TagsId") REFERENCES tags (id) ON DELETE CASCADE
);


CREATE TABLE user_achievement_stat_languages (
    id uuid NOT NULL,
    language_id uuid NOT NULL,
    user_achievement_stats_id uuid NOT NULL,
    CONSTRAINT "PK_user_achievement_stat_languages" PRIMARY KEY (id),
    CONSTRAINT "FK_user_achievement_stat_languages_languages_language_id" FOREIGN KEY (language_id) REFERENCES languages (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_user_achievement_stat_languages_user_achievement_stats_user~" FOREIGN KEY (user_achievement_stats_id) REFERENCES user_achievement_stats (id) ON DELETE CASCADE
);


CREATE TABLE unit_completions (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    campaign_unit_id uuid NOT NULL,
    completed_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_unit_completions" PRIMARY KEY (id),
    CONSTRAINT "FK_unit_completions_campaign_units_campaign_unit_id" FOREIGN KEY (campaign_unit_id) REFERENCES campaign_units (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_unit_completions_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);


CREATE TABLE unit_problems (
    id uuid NOT NULL,
    "CampaignUnitId" uuid NOT NULL,
    problem_id uuid NOT NULL,
    sort_order integer NOT NULL,
    campaign_unit_id uuid NOT NULL,
    CONSTRAINT "PK_unit_problems" PRIMARY KEY (id),
    CONSTRAINT "FK_unit_problems_campaign_units_campaign_unit_id" FOREIGN KEY (campaign_unit_id) REFERENCES campaign_units (id) ON DELETE CASCADE,
    CONSTRAINT "FK_unit_problems_problems_problem_id" FOREIGN KEY (problem_id) REFERENCES problems (id) ON DELETE RESTRICT
);


CREATE TABLE game_rounds (
    id uuid NOT NULL,
    game_id uuid NOT NULL,
    position integer NOT NULL,
    problem_id uuid NOT NULL,
    CONSTRAINT "PK_game_rounds" PRIMARY KEY (id),
    CONSTRAINT "AK_game_rounds_id_game_id" UNIQUE (id, game_id),
    CONSTRAINT "AK_game_rounds_id_problem_id_game_id" UNIQUE (id, problem_id, game_id),
    CONSTRAINT "FK_game_rounds_games_game_id" FOREIGN KEY (game_id) REFERENCES games (id) ON DELETE CASCADE,
    CONSTRAINT "FK_game_rounds_problems_problem_id" FOREIGN KEY (problem_id) REFERENCES problems (id) ON DELETE RESTRICT
);


CREATE TABLE game_tracks (
    id uuid NOT NULL,
    track_id uuid NOT NULL,
    game_id uuid NOT NULL,
    CONSTRAINT "PK_game_tracks" PRIMARY KEY (id),
    CONSTRAINT "FK_game_tracks_games_game_id" FOREIGN KEY (game_id) REFERENCES games (id) ON DELETE CASCADE,
    CONSTRAINT "FK_game_tracks_tracks_track_id" FOREIGN KEY (track_id) REFERENCES tracks (id) ON DELETE RESTRICT
);


CREATE TABLE leaderboard_participants (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    high_score integer NOT NULL DEFAULT 0,
    leaderboard_id uuid NOT NULL,
    CONSTRAINT "PK_leaderboard_participants" PRIMARY KEY (id),
    CONSTRAINT "FK_leaderboard_participants_leaderboards_leaderboard_id" FOREIGN KEY (leaderboard_id) REFERENCES leaderboards (id) ON DELETE CASCADE,
    CONSTRAINT "FK_leaderboard_participants_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);


CREATE TABLE problem_setups (
    id uuid NOT NULL,
    language_version_id uuid NOT NULL,
    pipeline_id uuid NOT NULL,
    initial_code text NOT NULL,
    function_name character varying(200),
    reference_solution_code text,
    generation_spec_id uuid,
    additional_file_bundle_id uuid,
    additional_files text NOT NULL,
    problem_id uuid NOT NULL,
    CONSTRAINT "PK_problem_setups" PRIMARY KEY (id),
    CONSTRAINT "AK_problem_setups_id_problem_id" UNIQUE (id, problem_id),
    CONSTRAINT "FK_problem_setups_additional_file_bundles_additional_file_bund~" FOREIGN KEY (additional_file_bundle_id) REFERENCES additional_file_bundles (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_problem_setups_execution_pipelines_pipeline_id" FOREIGN KEY (pipeline_id) REFERENCES execution_pipelines (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_problem_setups_language_versions_language_version_id" FOREIGN KEY (language_version_id) REFERENCES language_versions (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_problem_setups_problems_problem_id" FOREIGN KEY (problem_id) REFERENCES problems (id) ON DELETE CASCADE,
    CONSTRAINT "FK_problem_setups_test_case_generation_specs_generation_spec_id" FOREIGN KEY (generation_spec_id) REFERENCES test_case_generation_specs (id) ON DELETE RESTRICT
);


CREATE TABLE game_participants (
    id uuid NOT NULL,
    game_id uuid NOT NULL,
    user_id uuid NOT NULL,
    seat_no integer NOT NULL,
    joined_at timestamp with time zone NOT NULL,
    score integer NOT NULL DEFAULT 0,
    skips_remaining integer NOT NULL DEFAULT 3,
    forfeited_at timestamp with time zone,
    finished_at timestamp with time zone,
    current_round_id uuid,
    current_round_started_at timestamp with time zone,
    CONSTRAINT "PK_game_participants" PRIMARY KEY (id),
    CONSTRAINT "AK_game_participants_id_game_id" UNIQUE (id, game_id),
    CONSTRAINT "AK_game_participants_id_game_id_user_id" UNIQUE (id, game_id, user_id),
    CONSTRAINT ck_game_participants_current_round_pair CHECK ((current_round_id IS NULL) = (current_round_started_at IS NULL)),
    CONSTRAINT "FK_game_participants_game_rounds_current_round_id_game_id" FOREIGN KEY (current_round_id, game_id) REFERENCES game_rounds (id, game_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_game_participants_games_game_id" FOREIGN KEY (game_id) REFERENCES games (id) ON DELETE CASCADE,
    CONSTRAINT "FK_game_participants_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);


CREATE TABLE game_track_languages (
    id uuid NOT NULL,
    language_id uuid NOT NULL,
    game_track_id uuid NOT NULL,
    CONSTRAINT "PK_game_track_languages" PRIMARY KEY (id),
    CONSTRAINT "FK_game_track_languages_game_tracks_game_track_id" FOREIGN KEY (game_track_id) REFERENCES game_tracks (id) ON DELETE CASCADE,
    CONSTRAINT "FK_game_track_languages_languages_language_id" FOREIGN KEY (language_id) REFERENCES languages (id) ON DELETE RESTRICT
);


CREATE TABLE problem_setup_test_suites (
    problem_setup_id uuid NOT NULL,
    test_suite_id uuid NOT NULL,
    CONSTRAINT "PK_problem_setup_test_suites" PRIMARY KEY (problem_setup_id, test_suite_id),
    CONSTRAINT "FK_problem_setup_test_suites_problem_setups_problem_setup_id" FOREIGN KEY (problem_setup_id) REFERENCES problem_setups (id) ON DELETE CASCADE,
    CONSTRAINT "FK_problem_setup_test_suites_test_suites_test_suite_id" FOREIGN KEY (test_suite_id) REFERENCES test_suites (id) ON DELETE CASCADE
);


CREATE TABLE submissions (
    id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    problem_setup_id uuid NOT NULL,
    game_id uuid,
    source_code character varying(65536) NOT NULL,
    status integer NOT NULL,
    type integer NOT NULL,
    user_id uuid NOT NULL,
    additional_files text NOT NULL,
    CONSTRAINT "PK_submissions" PRIMARY KEY (id),
    CONSTRAINT "AK_submissions_id_problem_setup_id_user_id" UNIQUE (id, problem_setup_id, user_id),
    CONSTRAINT "FK_submissions_games_game_id" FOREIGN KEY (game_id) REFERENCES games (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_submissions_problem_setups_problem_setup_id" FOREIGN KEY (problem_setup_id) REFERENCES problem_setups (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_submissions_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);


CREATE TABLE game_round_results (
    id uuid NOT NULL,
    round_id uuid NOT NULL,
    outcome integer NOT NULL,
    started_at timestamp with time zone NOT NULL,
    completed_at timestamp with time zone NOT NULL,
    game_id uuid NOT NULL,
    participant_id uuid NOT NULL,
    CONSTRAINT "PK_game_round_results" PRIMARY KEY (id),
    CONSTRAINT "AK_game_round_results_id_outcome" UNIQUE (id, outcome),
    CONSTRAINT "FK_game_round_results_game_participants_participant_id_game_id" FOREIGN KEY (participant_id, game_id) REFERENCES game_participants (id, game_id) ON DELETE CASCADE,
    CONSTRAINT "FK_game_round_results_game_rounds_round_id_game_id" FOREIGN KEY (round_id, game_id) REFERENCES game_rounds (id, game_id) ON DELETE CASCADE
);


CREATE TABLE game_round_submissions (
    id uuid NOT NULL,
    round_id uuid NOT NULL,
    participant_id uuid NOT NULL,
    setup_id uuid NOT NULL,
    submission_id uuid NOT NULL,
    problem_id uuid NOT NULL,
    game_id uuid NOT NULL,
    user_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_game_round_submissions" PRIMARY KEY (id),
    CONSTRAINT "FK_game_round_submissions_game_participants_participant_id_gam~" FOREIGN KEY (participant_id, game_id, user_id) REFERENCES game_participants (id, game_id, user_id) ON DELETE CASCADE,
    CONSTRAINT "FK_game_round_submissions_game_rounds_round_id_problem_id_game~" FOREIGN KEY (round_id, problem_id, game_id) REFERENCES game_rounds (id, problem_id, game_id) ON DELETE CASCADE,
    CONSTRAINT "FK_game_round_submissions_problem_setups_setup_id_problem_id" FOREIGN KEY (setup_id, problem_id) REFERENCES problem_setups (id, problem_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_game_round_submissions_submissions_submission_id_setup_id_u~" FOREIGN KEY (submission_id, setup_id, user_id) REFERENCES submissions (id, problem_setup_id, user_id) ON DELETE CASCADE
);


CREATE TABLE submission_jobs (
    id uuid NOT NULL,
    submission_id uuid NOT NULL,
    pipeline_id uuid NOT NULL,
    current_step_id uuid,
    status integer NOT NULL,
    failure_reason text,
    created_at timestamp with time zone NOT NULL,
    completed_at timestamp with time zone,
    locked_until timestamp with time zone,
    CONSTRAINT "PK_submission_jobs" PRIMARY KEY (id),
    CONSTRAINT "FK_submission_jobs_execution_pipeline_steps_current_step_id" FOREIGN KEY (current_step_id) REFERENCES execution_pipeline_steps (id) ON DELETE SET NULL,
    CONSTRAINT "FK_submission_jobs_execution_pipelines_pipeline_id" FOREIGN KEY (pipeline_id) REFERENCES execution_pipelines (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_submission_jobs_submissions_submission_id" FOREIGN KEY (submission_id) REFERENCES submissions (id) ON DELETE CASCADE
);


CREATE TABLE submission_results (
    id uuid NOT NULL,
    actual_output text,
    compile_output text,
    memory_used integer,
    runtime integer,
    standard_error text,
    standard_output text,
    status integer NOT NULL,
    test_case_id uuid NOT NULL,
    evaluation_id character varying(100),
    execution_id character varying(100),
    submission_id uuid NOT NULL,
    CONSTRAINT "PK_submission_results" PRIMARY KEY (id),
    CONSTRAINT "FK_submission_results_submissions_submission_id" FOREIGN KEY (submission_id) REFERENCES submissions (id) ON DELETE CASCADE,
    CONSTRAINT "FK_submission_results_test_cases_test_case_id" FOREIGN KEY (test_case_id) REFERENCES test_cases (id) ON DELETE RESTRICT
);


CREATE TABLE game_round_solutions (
    id uuid NOT NULL,
    submission_id uuid NOT NULL,
    outcome integer NOT NULL,
    round_result_id uuid NOT NULL,
    CONSTRAINT "PK_game_round_solutions" PRIMARY KEY (id),
    CONSTRAINT ck_game_round_solutions_outcome CHECK (outcome = 0),
    CONSTRAINT "FK_game_round_solutions_game_round_results_round_result_id_out~" FOREIGN KEY (round_result_id, outcome) REFERENCES game_round_results (id, outcome) ON DELETE CASCADE
);


CREATE TABLE submission_job_attempts (
    id uuid NOT NULL,
    pipeline_step_id uuid NOT NULL,
    attempt_number integer NOT NULL,
    status integer NOT NULL,
    request_payload text,
    response_payload text,
    error text,
    started_at timestamp with time zone NOT NULL,
    completed_at timestamp with time zone,
    duration_ms integer,
    job_id uuid NOT NULL,
    CONSTRAINT "PK_submission_job_attempts" PRIMARY KEY (id),
    CONSTRAINT "FK_submission_job_attempts_execution_pipeline_steps_pipeline_s~" FOREIGN KEY (pipeline_step_id) REFERENCES execution_pipeline_steps (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_submission_job_attempts_submission_jobs_job_id" FOREIGN KEY (job_id) REFERENCES submission_jobs (id) ON DELETE CASCADE
);


CREATE UNIQUE INDEX "IX_achievement_definitions_code" ON achievement_definitions (code);


CREATE UNIQUE INDEX "IX_additional_file_bundles_name" ON additional_file_bundles (name);


CREATE UNIQUE INDEX "IX_assert_step_configurations_pipeline_step_id" ON assert_step_configurations (pipeline_step_id);


CREATE INDEX ix_audit_log_entries_actor_user_id ON audit_log_entries (actor_user_id);


CREATE INDEX ix_audit_log_entries_created_at ON audit_log_entries (created_at);


CREATE INDEX "IX_campaign_enrollments_campaign_id" ON campaign_enrollments (campaign_id);


CREATE UNIQUE INDEX "IX_campaign_enrollments_user_id_campaign_id" ON campaign_enrollments (user_id, campaign_id);


CREATE INDEX "IX_campaign_modules_campaign_id_sort_order" ON campaign_modules (campaign_id, sort_order);


CREATE UNIQUE INDEX "IX_campaign_prerequisites_campaign_id_required_campaign_id" ON campaign_prerequisites (campaign_id, required_campaign_id);


CREATE INDEX "IX_campaign_prerequisites_required_campaign_id" ON campaign_prerequisites (required_campaign_id);


CREATE INDEX "IX_campaign_units_campaign_module_id_sort_order" ON campaign_units (campaign_module_id, sort_order);


CREATE UNIQUE INDEX "IX_campaigns_slug" ON campaigns (slug);


CREATE UNIQUE INDEX "IX_daily_challenges_challenge_date" ON daily_challenges (challenge_date);


CREATE INDEX "IX_daily_challenges_problem_id" ON daily_challenges (problem_id);


CREATE INDEX "IX_execution_pipeline_steps_pipeline_id" ON execution_pipeline_steps (pipeline_id);


CREATE INDEX "IX_feature_flag_group_overrides_feature_flag_id" ON feature_flag_group_overrides (feature_flag_id);


CREATE INDEX "IX_feature_flag_user_overrides_feature_flag_id" ON feature_flag_user_overrides (feature_flag_id);


CREATE UNIQUE INDEX "IX_feature_flags_key" ON feature_flags (key);


CREATE INDEX "IX_feedback_submissions_user_id" ON feedback_submissions (user_id);


CREATE UNIQUE INDEX "IX_game_mode_time_options_game_mode_id_duration_seconds" ON game_mode_time_options (game_mode_id, duration_seconds);


CREATE INDEX "IX_game_modes_default_pool_id" ON game_modes (default_pool_id);


CREATE UNIQUE INDEX "IX_game_modes_key" ON game_modes (key);


CREATE INDEX "IX_game_participants_current_round_id_game_id" ON game_participants (current_round_id, game_id);


CREATE UNIQUE INDEX "IX_game_participants_game_id_seat_no" ON game_participants (game_id, seat_no);


CREATE UNIQUE INDEX "IX_game_participants_game_id_user_id" ON game_participants (game_id, user_id);


CREATE INDEX "IX_game_participants_user_id" ON game_participants (user_id);


CREATE INDEX "IX_game_round_results_participant_id_game_id" ON game_round_results (participant_id, game_id);


CREATE UNIQUE INDEX "IX_game_round_results_participant_id_round_id" ON game_round_results (participant_id, round_id);


CREATE INDEX "IX_game_round_results_round_id_game_id" ON game_round_results (round_id, game_id);


CREATE UNIQUE INDEX "IX_game_round_solutions_round_result_id" ON game_round_solutions (round_result_id);


CREATE UNIQUE INDEX "IX_game_round_solutions_round_result_id_outcome" ON game_round_solutions (round_result_id, outcome);


CREATE INDEX "IX_game_round_submissions_participant_id_game_id_user_id" ON game_round_submissions (participant_id, game_id, user_id);


CREATE INDEX "IX_game_round_submissions_participant_id_round_id_created_at" ON game_round_submissions (participant_id, round_id, created_at);


CREATE INDEX "IX_game_round_submissions_round_id_problem_id_game_id" ON game_round_submissions (round_id, problem_id, game_id);


CREATE INDEX "IX_game_round_submissions_setup_id_problem_id" ON game_round_submissions (setup_id, problem_id);


CREATE UNIQUE INDEX "IX_game_round_submissions_submission_id" ON game_round_submissions (submission_id);


CREATE INDEX "IX_game_round_submissions_submission_id_setup_id_user_id" ON game_round_submissions (submission_id, setup_id, user_id);


CREATE UNIQUE INDEX "IX_game_rounds_game_id_position" ON game_rounds (game_id, position);


CREATE UNIQUE INDEX "IX_game_rounds_game_id_problem_id" ON game_rounds (game_id, problem_id);


CREATE INDEX "IX_game_rounds_problem_id" ON game_rounds (problem_id);


CREATE UNIQUE INDEX "IX_game_track_languages_game_track_id_language_id" ON game_track_languages (game_track_id, language_id);


CREATE INDEX "IX_game_track_languages_language_id" ON game_track_languages (language_id);


CREATE UNIQUE INDEX "IX_game_tracks_game_id_track_id" ON game_tracks (game_id, track_id);


CREATE INDEX "IX_game_tracks_track_id" ON game_tracks (track_id);


CREATE INDEX "IX_games_game_mode_id" ON games (game_mode_id);


CREATE UNIQUE INDEX "IX_games_join_code" ON games (join_code);


CREATE INDEX "IX_games_pool_id" ON games (pool_id);


CREATE INDEX "IX_group_roles_role_id" ON group_roles (role_id);


CREATE UNIQUE INDEX "IX_judge0_step_configurations_pipeline_step_id" ON judge0_step_configurations (pipeline_step_id);


CREATE INDEX "IX_language_versions_language_id" ON language_versions (language_id);


CREATE INDEX "IX_languages_track_id" ON languages (track_id);


CREATE UNIQUE INDEX "IX_leaderboard_participants_leaderboard_id_user_id" ON leaderboard_participants (leaderboard_id, user_id);


CREATE INDEX "IX_leaderboard_participants_user_id" ON leaderboard_participants (user_id);


CREATE UNIQUE INDEX "IX_leaderboards_game_mode_id_time_limit_in_seconds" ON leaderboards (game_mode_id, time_limit_in_seconds);


CREATE UNIQUE INDEX "IX_permissions_code" ON permissions (code);


CREATE INDEX "IX_problem_history_problem_id" ON problem_history (problem_id);


CREATE UNIQUE INDEX "IX_problem_pool_problems_pool_id_problem_id" ON problem_pool_problems (pool_id, problem_id);


CREATE INDEX "IX_problem_pool_problems_problem_id" ON problem_pool_problems (problem_id);


CREATE UNIQUE INDEX "IX_problem_pools_key" ON problem_pools (key);


CREATE UNIQUE INDEX "IX_problem_reaction_types_key" ON problem_reaction_types (key);


CREATE UNIQUE INDEX ix_problem_reactions_active_per_user ON problem_reactions (problem_id, user_id) WHERE is_active = true;


CREATE UNIQUE INDEX "IX_problem_reactions_problem_id_user_id_reaction_type_id" ON problem_reactions (problem_id, user_id, reaction_type_id);


CREATE INDEX "IX_problem_reactions_reaction_type_id" ON problem_reactions (reaction_type_id);


CREATE INDEX "IX_problem_reactions_user_id" ON problem_reactions (user_id);


CREATE INDEX "IX_problem_setup_test_suites_test_suite_id" ON problem_setup_test_suites (test_suite_id);


CREATE INDEX "IX_problem_setups_additional_file_bundle_id" ON problem_setups (additional_file_bundle_id);


CREATE INDEX "IX_problem_setups_generation_spec_id" ON problem_setups (generation_spec_id);


CREATE INDEX "IX_problem_setups_language_version_id" ON problem_setups (language_version_id);


CREATE INDEX "IX_problem_setups_pipeline_id" ON problem_setups (pipeline_id);


CREATE INDEX "IX_problem_setups_problem_id" ON problem_setups (problem_id);


CREATE INDEX "IX_problem_tags_TagsId" ON problem_tags ("TagsId");


CREATE INDEX "IX_problem_validation_jobs_status_locked_until" ON problem_validation_jobs (status, locked_until);


CREATE INDEX "IX_problems_created_by_id" ON problems (created_by_id);


CREATE UNIQUE INDEX "IX_problems_slug" ON problems (slug);


CREATE INDEX "IX_problems_track_id" ON problems (track_id);


CREATE UNIQUE INDEX "IX_required_problem_languages_language_version_id" ON required_problem_languages (language_version_id);


CREATE INDEX "IX_role_permissions_permission_id" ON role_permissions (permission_id);


CREATE INDEX "IX_role_permissions_role_id" ON role_permissions (role_id);


CREATE INDEX "IX_security_restrictions_UserId_PermissionCode" ON security_restrictions ("UserId", "PermissionCode");


CREATE INDEX "IX_submission_job_attempts_job_id" ON submission_job_attempts (job_id);


CREATE INDEX "IX_submission_job_attempts_pipeline_step_id" ON submission_job_attempts (pipeline_step_id);


CREATE INDEX "IX_submission_jobs_current_step_id" ON submission_jobs (current_step_id);


CREATE INDEX "IX_submission_jobs_pipeline_id" ON submission_jobs (pipeline_id);


CREATE INDEX "IX_submission_jobs_status_locked_until_created_at" ON submission_jobs (status, locked_until, created_at);


CREATE UNIQUE INDEX "IX_submission_jobs_submission_id" ON submission_jobs (submission_id);


CREATE INDEX "IX_submission_results_submission_id" ON submission_results (submission_id);


CREATE INDEX "IX_submission_results_test_case_id" ON submission_results (test_case_id);


CREATE INDEX "IX_submissions_game_id" ON submissions (game_id);


CREATE INDEX "IX_submissions_problem_setup_id" ON submissions (problem_setup_id);


CREATE INDEX "IX_submissions_user_id" ON submissions (user_id);


CREATE UNIQUE INDEX "IX_tags_name" ON tags (name);


CREATE INDEX "IX_test_case_expected_outputs_test_case_id" ON test_case_expected_outputs (test_case_id);


CREATE INDEX "IX_test_case_generation_jobs_status" ON test_case_generation_jobs (status);


CREATE INDEX "IX_test_case_inputs_test_case_id" ON test_case_inputs (test_case_id);


CREATE INDEX "IX_test_cases_generation_spec_id" ON test_cases (generation_spec_id);


CREATE INDEX "IX_test_cases_test_suite_id" ON test_cases (test_suite_id);


CREATE UNIQUE INDEX "IX_tracks_key" ON tracks (key);


CREATE INDEX "IX_unit_completions_campaign_unit_id" ON unit_completions (campaign_unit_id);


CREATE UNIQUE INDEX "IX_unit_completions_user_id_campaign_unit_id" ON unit_completions (user_id, campaign_unit_id);


CREATE UNIQUE INDEX "IX_unit_problems_campaign_unit_id_problem_id" ON unit_problems (campaign_unit_id, problem_id);


CREATE INDEX "IX_unit_problems_problem_id" ON unit_problems (problem_id);


CREATE INDEX "IX_user_achievement_stat_languages_language_id" ON user_achievement_stat_languages (language_id);


CREATE UNIQUE INDEX "IX_user_achievement_stat_languages_user_achievement_stats_id_l~" ON user_achievement_stat_languages (user_achievement_stats_id, language_id);


CREATE UNIQUE INDEX "IX_user_achievement_stats_user_id" ON user_achievement_stats (user_id);


CREATE INDEX "IX_user_achievements_achievement_definition_id" ON user_achievements (achievement_definition_id);


CREATE UNIQUE INDEX "IX_user_achievements_user_id_achievement_definition_id" ON user_achievements (user_id, achievement_definition_id);


CREATE INDEX "IX_user_avatars_user_id_created_at" ON user_avatars (user_id, created_at);


CREATE INDEX "IX_user_groups_group_id" ON user_groups (group_id);


CREATE INDEX "IX_user_language_preferences_language_id" ON user_language_preferences (language_id);


CREATE UNIQUE INDEX "IX_user_language_preferences_user_id_position" ON user_language_preferences (user_id, position);


CREATE UNIQUE INDEX "IX_users_sub" ON users (sub);


CREATE UNIQUE INDEX "IX_users_username" ON users (username);


