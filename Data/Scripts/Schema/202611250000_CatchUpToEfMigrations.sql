CREATE TABLE problem_validation_steps (
    id uuid NOT NULL,
    kind integer NOT NULL,
    status integer NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    detail character varying(2000),
    problem_validation_job_id uuid NOT NULL,
    CONSTRAINT "PK_problem_validation_steps" PRIMARY KEY (id),
    CONSTRAINT "FK_problem_validation_steps_problem_validation_jobs_problem_va~" FOREIGN KEY (problem_validation_job_id) REFERENCES problem_validation_jobs (id) ON DELETE CASCADE
);

CREATE TABLE problem_validation_step_items (
    id uuid NOT NULL,
    label character varying(200) NOT NULL,
    status integer NOT NULL,
    detail character varying(2000),
    problem_validation_step_id uuid NOT NULL,
    CONSTRAINT "PK_problem_validation_step_items" PRIMARY KEY (id),
    CONSTRAINT "FK_problem_validation_step_items_problem_validation_steps_prob~" FOREIGN KEY (problem_validation_step_id) REFERENCES problem_validation_steps (id) ON DELETE CASCADE
);

CREATE INDEX "IX_problem_validation_jobs_problem_id_created_at" ON problem_validation_jobs (problem_id, created_at);

CREATE INDEX "IX_problem_validation_step_items_problem_validation_step_id" ON problem_validation_step_items (problem_validation_step_id);

CREATE INDEX "IX_problem_validation_steps_problem_validation_job_id" ON problem_validation_steps (problem_validation_job_id);

ALTER TABLE problem_validation_jobs ADD last_error character varying(2000);

ALTER TABLE problem_validation_jobs ADD processing_attempts integer NOT NULL DEFAULT 0;

ALTER TABLE submissions ADD problem_version_id uuid;

ALTER TABLE problems ADD published_version_id uuid;

CREATE TABLE problem_versions (
    id uuid NOT NULL,
    problem_id uuid NOT NULL,
    version_number integer NOT NULL,
    state integer NOT NULL,
    title character varying(200) NOT NULL,
    question character varying(10000) NOT NULL,
    difficulty integer NOT NULL,
    time_limit integer NOT NULL,
    memory_limit integer NOT NULL,
    published_at timestamp with time zone NOT NULL,
    superseded_at timestamp with time zone,
    CONSTRAINT "PK_problem_versions" PRIMARY KEY (id),
    CONSTRAINT "FK_problem_versions_problems_problem_id" FOREIGN KEY (problem_id) REFERENCES problems (id) ON DELETE CASCADE
);

CREATE TABLE problem_version_setups (
    id uuid NOT NULL,
    problem_setup_id uuid NOT NULL,
    problem_version_id uuid NOT NULL,
    CONSTRAINT "PK_problem_version_setups" PRIMARY KEY (id),
    CONSTRAINT "FK_problem_version_setups_problem_setups_problem_setup_id" FOREIGN KEY (problem_setup_id) REFERENCES problem_setups (id) ON DELETE CASCADE,
    CONSTRAINT "FK_problem_version_setups_problem_versions_problem_version_id" FOREIGN KEY (problem_version_id) REFERENCES problem_versions (id) ON DELETE CASCADE
);

CREATE INDEX "IX_submissions_problem_version_id" ON submissions (problem_version_id);

CREATE INDEX "IX_problem_version_setups_problem_setup_id" ON problem_version_setups (problem_setup_id);

CREATE INDEX "IX_problem_version_setups_problem_version_id" ON problem_version_setups (problem_version_id);

CREATE UNIQUE INDEX "IX_problem_versions_problem_id_version_number" ON problem_versions (problem_id, version_number);

ALTER TABLE submissions ADD CONSTRAINT "FK_submissions_problem_versions_problem_version_id" FOREIGN KEY (problem_version_id) REFERENCES problem_versions (id) ON DELETE RESTRICT;

insert into problem_versions (
    id, problem_id, version_number, state, title, question,
    difficulty, time_limit, memory_limit, published_at, superseded_at
)
select gen_random_uuid(), p.id, 1, 1, p.title, p.question,
       p.difficulty, p.time_limit, p.memory_limit, p.created_at, null
from problems p
where p.status in (2, 3);

update problems p
set published_version_id = v.id
from problem_versions v
where v.problem_id = p.id and v.version_number = 1;

insert into problem_version_setups (id, problem_setup_id, problem_version_id)
select gen_random_uuid(), s.id, v.id
from problem_setups s
join problem_versions v on v.problem_id = s.problem_id;

update submissions sub
set problem_version_id = p.published_version_id
from problem_setups s
join problems p on p.id = s.problem_id
where sub.problem_setup_id = s.id and p.published_version_id is not null;

ALTER TABLE problem_version_setups ADD function_name character varying(200);

ALTER TABLE problem_version_setups ADD initial_code text NOT NULL DEFAULT '';

ALTER TABLE problem_version_setups ADD language_version_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

update problem_version_setups vs
set language_version_id = s.language_version_id,
    initial_code = s.initial_code,
    function_name = s.function_name
from problem_setups s
where s.id = vs.problem_setup_id;

ALTER TABLE game_rounds ADD problem_version_id uuid;

CREATE INDEX "IX_game_rounds_problem_version_id" ON game_rounds (problem_version_id);

ALTER TABLE game_rounds ADD CONSTRAINT "FK_game_rounds_problem_versions_problem_version_id" FOREIGN KEY (problem_version_id) REFERENCES problem_versions (id) ON DELETE RESTRICT;

update game_rounds r
set problem_version_id = p.published_version_id
from problems p
where p.id = r.problem_id and p.published_version_id is not null;

ALTER TABLE problems ADD has_unpublished_changes boolean NOT NULL DEFAULT FALSE;

ALTER TABLE problems ADD is_revalidating boolean NOT NULL DEFAULT FALSE;

ALTER TABLE problems ADD deleted_at timestamp with time zone;

DROP INDEX "IX_problems_slug";

CREATE UNIQUE INDEX "IX_problems_slug" ON problems (slug) WHERE deleted_at IS NULL;

ALTER TABLE problem_setups ADD retired_at timestamp with time zone;

ALTER TABLE problem_version_setups DROP CONSTRAINT "FK_problem_version_setups_problem_setups_problem_setup_id";

ALTER TABLE problem_version_setups ADD CONSTRAINT "FK_problem_version_setups_problem_setups_problem_setup_id" FOREIGN KEY (problem_setup_id) REFERENCES problem_setups (id) ON DELETE RESTRICT;
