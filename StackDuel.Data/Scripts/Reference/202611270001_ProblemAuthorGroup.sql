INSERT INTO public.permissions (id, code, description) VALUES ('59db0890-10e0-412a-909e-47c3963ebe6d', 'problem:update:studio', 'Allows editing your own studio problem drafts and setups') ON CONFLICT DO NOTHING;



INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('59db0890-10e0-412a-909e-47c3963ebe6d', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;



INSERT INTO public.groups (id, name) VALUES ('a5150e41-5155-42a0-9acf-7cdf9e24b1ba', 'problem-author') ON CONFLICT DO NOTHING;



INSERT INTO public.roles (id, name) VALUES ('1377b9d6-fc9f-4848-ab7c-f568bb760a06', 'problem-author') ON CONFLICT DO NOTHING;



INSERT INTO public.group_roles (group_id, role_id) VALUES ('a5150e41-5155-42a0-9acf-7cdf9e24b1ba', '1377b9d6-fc9f-4848-ab7c-f568bb760a06') ON CONFLICT DO NOTHING;



INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('092ba0ab-9f59-44d6-9544-d4e0f114b4a3', '1377b9d6-fc9f-4848-ab7c-f568bb760a06', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('cf126609-a88e-49ad-84e9-c0111dd20efb', '1377b9d6-fc9f-4848-ab7c-f568bb760a06', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('59db0890-10e0-412a-909e-47c3963ebe6d', '1377b9d6-fc9f-4848-ab7c-f568bb760a06', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('ef2c7c12-ee1e-4d24-a953-3b7455535363', '1377b9d6-fc9f-4848-ab7c-f568bb760a06', 'Allow') ON CONFLICT DO NOTHING;
