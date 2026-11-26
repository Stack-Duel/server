INSERT INTO public.tracks (id, key, name, is_active, allows_language_selection) VALUES ('06a25f31-197f-4931-87f3-686e3b4bc153', 'frontend', 'Frontend', false, true) ON CONFLICT DO NOTHING;
INSERT INTO public.tracks (id, key, name, is_active, allows_language_selection) VALUES ('b33287e4-153d-4f8a-a3e3-8cbfbcea1454', 'sql', 'SQL', true, false) ON CONFLICT DO NOTHING;
INSERT INTO public.tracks (id, key, name, is_active, allows_language_selection) VALUES ('c4ca3593-673d-4440-94ed-a59410479c4c', 'general-purpose', 'General Purpose', true, false) ON CONFLICT DO NOTHING;
