INSERT INTO public.execution_pipelines (id, name, description) VALUES ('2d8d6f9f-0cb6-4c85-924e-3038f7f89f58', 'Default Judge0 Pipeline', 'Backfilled by migration to satisfy existing problem setup pipeline references.') ON CONFLICT DO NOTHING;



INSERT INTO public.execution_pipeline_steps (id, name, step_type, step_order, max_attempts, timeout_seconds, is_polling, pipeline_id) VALUES ('3dce6c09-d787-4bc0-a726-d87e5ff4048b', 'Submit to Judge0', 1, 1, 3, 30, false, '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58') ON CONFLICT DO NOTHING;
INSERT INTO public.execution_pipeline_steps (id, name, step_type, step_order, max_attempts, timeout_seconds, is_polling, pipeline_id) VALUES ('05f67946-d987-4712-b516-7f02357df3e6', 'Poll Judge0 Results', 2, 2, 30, 1, true, '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58') ON CONFLICT DO NOTHING;
INSERT INTO public.execution_pipeline_steps (id, name, step_type, step_order, max_attempts, timeout_seconds, is_polling, pipeline_id) VALUES ('45452bff-3791-4f87-9641-dc9fb686e065', 'Evaluate Results', 3, 3, 1, 10, false, '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58') ON CONFLICT DO NOTHING;
