INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('0744578f-5301-4d6b-8684-833978dc07db', 'Java', 'java', 0, 'c4ca3593-673d-4440-94ed-a59410479c4c') ON CONFLICT DO NOTHING;
INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('075ba2e7-3f48-41c3-a985-f5beef59af50', 'TypeScript', 'typescript', 1, 'c4ca3593-673d-4440-94ed-a59410479c4c') ON CONFLICT DO NOTHING;
INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('07b994a8-edb3-41ea-b6f1-e19f016cfe5f', 'Angular', 'angular', 1, '06a25f31-197f-4931-87f3-686e3b4bc153') ON CONFLICT DO NOTHING;
INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('391d9922-70a4-4962-bc69-fbb14df5f427', 'C++', 'cpp', 0, 'c4ca3593-673d-4440-94ed-a59410479c4c') ON CONFLICT DO NOTHING;
INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('93a10d0c-f915-406d-85d4-bf42345c188f', 'Vanilla JS', 'vanilla-js', 1, '06a25f31-197f-4931-87f3-686e3b4bc153') ON CONFLICT DO NOTHING;
INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('bb54a0c1-7ff6-45aa-874b-27fab1806d97', 'Python', 'python', 0, 'c4ca3593-673d-4440-94ed-a59410479c4c') ON CONFLICT DO NOTHING;
INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('c74702ce-de96-451a-b217-7b5657df97b8', 'SQLite', 'sqlite', 0, 'b33287e4-153d-4f8a-a3e3-8cbfbcea1454') ON CONFLICT DO NOTHING;
INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('d5aca7aa-6064-4250-8f61-8360b766beec', 'React', 'react', 1, '06a25f31-197f-4931-87f3-686e3b4bc153') ON CONFLICT DO NOTHING;
INSERT INTO public.languages (id, name, slug, status, track_id) VALUES ('e5bbaefb-715f-4783-8d2c-6af51de264b8', 'JavaScript', 'javascript', 0, 'c4ca3593-673d-4440-94ed-a59410479c4c') ON CONFLICT DO NOTHING;



INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('00380d11-5da4-4cff-b2f7-8d0dc0f5acab', 89, 1, '19.0.0', 'd5aca7aa-6064-4250-8f61-8360b766beec', 0) ON CONFLICT DO NOTHING;
INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 102, 0, 'Node.js 22.08.0', 'e5bbaefb-715f-4783-8d2c-6af51de264b8', 0) ON CONFLICT DO NOTHING;
INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('5081b28c-a96b-448f-baec-d5bfdd8bcc85', 109, 0, '3.13.2', 'bb54a0c1-7ff6-45aa-874b-27fab1806d97', 0) ON CONFLICT DO NOTHING;
INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('95a7bae2-0233-404d-9a8d-be2d876f4df0', 89, 1, 'ES2023', '93a10d0c-f915-406d-85d4-bf42345c188f', 0) ON CONFLICT DO NOTHING;
INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('a3091d0c-a87b-4317-9a29-4c8a0e5ee62f', 82, 0, '3.27.2', 'c74702ce-de96-451a-b217-7b5657df97b8', 0) ON CONFLICT DO NOTHING;
INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('b8d06bb8-d5b7-4c44-94a3-97dd536a8b13', 101, 1, '5.6.2', '075ba2e7-3f48-41c3-a985-f5beef59af50', 0) ON CONFLICT DO NOTHING;
INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('c094fc3b-b29e-46de-9273-007dd1f74c08', 89, 1, '18.2.0', '07b994a8-edb3-41ea-b6f1-e19f016cfe5f', 0) ON CONFLICT DO NOTHING;
INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('c6d6c86f-7c03-4503-bd55-4877d6165b0e', 54, 0, '(GCC 9.2.0)', '391d9922-70a4-4962-bc69-fbb14df5f427', 0) ON CONFLICT DO NOTHING;
INSERT INTO public.language_versions (id, judge0_id, status, version, language_id, execution_mode) VALUES ('eeb2be35-a342-4573-8007-880fa4f97ba2', 89, 0, 'JDK 17.0.6', '0744578f-5301-4d6b-8684-833978dc07db', 0) ON CONFLICT DO NOTHING;



INSERT INTO public.required_problem_languages (id, language_version_id, sort_order, created_at) VALUES ('baf9d879-5e32-4d8f-acaa-6af51b652fd7', '1f9a917b-a5a7-4d0d-856c-3b63b13d08aa', 1, '2026-11-21 20:31:34.096189+00') ON CONFLICT DO NOTHING;
INSERT INTO public.required_problem_languages (id, language_version_id, sort_order, created_at) VALUES ('f4d41217-8701-497b-afd1-38731be9b7a6', '5081b28c-a96b-448f-baec-d5bfdd8bcc85', 0, '2026-11-21 20:31:34.08902+00') ON CONFLICT DO NOTHING;
