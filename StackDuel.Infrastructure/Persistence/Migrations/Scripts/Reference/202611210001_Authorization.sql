INSERT INTO public.groups (id, name) VALUES ('42a34b0c-b5ac-4932-aa84-f1f528c40eeb', 'default-user') ON CONFLICT DO NOTHING;
INSERT INTO public.groups (id, name) VALUES ('59966085-3383-4bec-974a-4686b842ce5b', 'admin') ON CONFLICT DO NOTHING;



INSERT INTO public.roles (id, name) VALUES ('47d0dd40-972c-487e-8771-e4949a99bd21', 'admin') ON CONFLICT DO NOTHING;
INSERT INTO public.roles (id, name) VALUES ('97b66d25-061c-4067-8223-bb4328c8cf11', 'default-user') ON CONFLICT DO NOTHING;



INSERT INTO public.group_roles (group_id, role_id) VALUES ('42a34b0c-b5ac-4932-aa84-f1f528c40eeb', '97b66d25-061c-4067-8223-bb4328c8cf11') ON CONFLICT DO NOTHING;
INSERT INTO public.group_roles (group_id, role_id) VALUES ('59966085-3383-4bec-974a-4686b842ce5b', '47d0dd40-972c-487e-8771-e4949a99bd21') ON CONFLICT DO NOTHING;



INSERT INTO public.permissions (id, code, description) VALUES ('092ba0ab-9f59-44d6-9544-d4e0f114b4a3', 'problem:create:admin', 'Allows creating and authoring problems in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('10afe783-a3bb-4558-9dbe-99744a7f3936', 'user:read:admin', 'Allows viewing users in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('11c84dc3-2c36-4f48-bb08-57e4dfe9ff62', 'problem-required-language:manage:admin', 'Allows managing the languages required for new problems') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('1ae3c8d5-54c2-420a-96a5-b5befdb52d8d', 'game:solo-rush:play', 'Allows playing Solo Rush mode') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('238665f3-367f-42f9-a288-de6a3960596f', 'feedback:read:admin', 'Allows viewing feedback in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('420b33d6-208e-4e4c-9ffc-93aea68be745', 'feedback:update:admin', 'Allows updating feedback status in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('46f03c16-1639-4ff4-9066-a884f16661a5', 'dashboard:read:admin', 'Allows viewing the admin dashboard overview') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('4a1df623-0814-4c98-9c83-824ae0f935e1', 'problem:update:admin', 'Allows updating problems in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('541f115f-e3c3-4c8f-a502-2af5224a6a2b', 'game:duel:play', 'Allows playing Duel mode') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('69592280-2b7a-4375-956e-244bac0370d1', 'submission:view', 'Allows viewing code submissions') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('7032fcd1-fa93-4618-b3d9-f711f5d100e1', 'campaign:manage:admin', 'Allows creating and editing campaigns in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('750769c3-ffa4-4d34-9e51-31dcef2eab5b', 'user:groups:update:admin', 'Allows updating a user''s group memberships in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('7cc36fa5-5a75-4d17-86e6-c6bc72194609', 'games:read:admin', 'Allows viewing games in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('995dcf90-a193-4410-a893-e1d6216482bc', 'audit-log:read:admin', 'Allows viewing the admin audit log') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('9d499180-9757-4f1a-a87d-d954dabe3781', 'game:ffa:play', 'Allows playing FFA mode') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('a0b1fa00-a5fd-41c5-afe1-37e762c3187d', 'submission:create', 'Allows creating code submissions') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('a37168be-8eab-4b7a-9ed8-57bef361e70c', 'feedback:create', 'Allows submitting product feedback') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('ade4f086-f00d-403b-bc7a-51a26523c7cb', 'problem:read:admin', 'Allows viewing admin-only problem data') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('cf126609-a88e-49ad-84e9-c0111dd20efb', 'problem:read:studio', 'Allows viewing your own studio problem drafts and setups') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('cf766105-e42c-40bf-8a6f-1d55ae6e3825', 'campaign:read:admin', 'Allows viewing campaigns in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('ef2c7c12-ee1e-4d24-a953-3b7455535363', 'problem:submit:admin', 'Allows submitting an authored problem for validation') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('f4e071f6-13d7-4114-a7c7-983f0b51d414', 'feature-flag:manage:admin', 'Allows managing feature flags in the admin panel') ON CONFLICT DO NOTHING;
INSERT INTO public.permissions (id, code, description) VALUES ('ff952eac-67eb-4d6d-8c9b-cbb39b1e1afb', 'submission:read:admin', 'Allows viewing submissions in the admin panel') ON CONFLICT DO NOTHING;



INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('092ba0ab-9f59-44d6-9544-d4e0f114b4a3', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('10afe783-a3bb-4558-9dbe-99744a7f3936', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('11c84dc3-2c36-4f48-bb08-57e4dfe9ff62', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('1ae3c8d5-54c2-420a-96a5-b5befdb52d8d', '97b66d25-061c-4067-8223-bb4328c8cf11', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('238665f3-367f-42f9-a288-de6a3960596f', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('420b33d6-208e-4e4c-9ffc-93aea68be745', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('46f03c16-1639-4ff4-9066-a884f16661a5', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('4a1df623-0814-4c98-9c83-824ae0f935e1', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('541f115f-e3c3-4c8f-a502-2af5224a6a2b', '97b66d25-061c-4067-8223-bb4328c8cf11', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('69592280-2b7a-4375-956e-244bac0370d1', '97b66d25-061c-4067-8223-bb4328c8cf11', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('7032fcd1-fa93-4618-b3d9-f711f5d100e1', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('750769c3-ffa4-4d34-9e51-31dcef2eab5b', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('7cc36fa5-5a75-4d17-86e6-c6bc72194609', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('995dcf90-a193-4410-a893-e1d6216482bc', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('9d499180-9757-4f1a-a87d-d954dabe3781', '97b66d25-061c-4067-8223-bb4328c8cf11', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('a0b1fa00-a5fd-41c5-afe1-37e762c3187d', '97b66d25-061c-4067-8223-bb4328c8cf11', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('a37168be-8eab-4b7a-9ed8-57bef361e70c', '97b66d25-061c-4067-8223-bb4328c8cf11', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('ade4f086-f00d-403b-bc7a-51a26523c7cb', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('cf126609-a88e-49ad-84e9-c0111dd20efb', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('cf766105-e42c-40bf-8a6f-1d55ae6e3825', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('ef2c7c12-ee1e-4d24-a953-3b7455535363', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('f4e071f6-13d7-4114-a7c7-983f0b51d414', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
INSERT INTO public.role_permissions (permission_id, role_id, effect) VALUES ('ff952eac-67eb-4d6d-8c9b-cbb39b1e1afb', '47d0dd40-972c-487e-8771-e4949a99bd21', 'Allow') ON CONFLICT DO NOTHING;
