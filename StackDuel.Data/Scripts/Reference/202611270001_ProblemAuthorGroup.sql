INSERT INTO public.permissions (id, code, description)
SELECT '59db0890-10e0-412a-909e-47c3963ebe6d', 'problem:update:studio', 'Allows editing your own studio problem drafts and setups'
WHERE NOT EXISTS (SELECT 1 FROM public.permissions WHERE code = 'problem:update:studio');

INSERT INTO public.permissions (id, code, description)
SELECT '7c244d88-2cda-4af3-a655-02a8174464e0', 'problem:create:admin', 'Allows creating and authoring problems in the admin panel'
WHERE NOT EXISTS (SELECT 1 FROM public.permissions WHERE code = 'problem:create:admin');

INSERT INTO public.permissions (id, code, description)
SELECT 'dc0af930-cb51-48a9-b8d6-3520c1b19538', 'problem:read:studio', 'Allows viewing your own studio problem drafts and setups'
WHERE NOT EXISTS (SELECT 1 FROM public.permissions WHERE code = 'problem:read:studio');

INSERT INTO public.permissions (id, code, description)
SELECT '22943e0a-3fb5-474e-9421-d153ea7d250c', 'problem:submit:admin', 'Allows submitting an authored problem for validation'
WHERE NOT EXISTS (SELECT 1 FROM public.permissions WHERE code = 'problem:submit:admin');



INSERT INTO public.groups (id, name)
SELECT 'a5150e41-5155-42a0-9acf-7cdf9e24b1ba', 'problem-author'
WHERE NOT EXISTS (SELECT 1 FROM public.groups WHERE name = 'problem-author');

INSERT INTO public.roles (id, name)
SELECT '1377b9d6-fc9f-4848-ab7c-f568bb760a06', 'problem-author'
WHERE NOT EXISTS (SELECT 1 FROM public.roles WHERE name = 'problem-author');

INSERT INTO public.group_roles (group_id, role_id)
SELECT g.id, r.id
FROM public.groups g
JOIN public.roles r ON r.name = 'problem-author'
WHERE g.name = 'problem-author'
ON CONFLICT DO NOTHING;



INSERT INTO public.role_permissions (permission_id, role_id, effect)
SELECT p.id, r.id, 'Allow'
FROM public.permissions p
JOIN public.roles r ON r.name = 'admin'
WHERE p.code = 'problem:update:studio'
ON CONFLICT DO NOTHING;

INSERT INTO public.role_permissions (permission_id, role_id, effect)
SELECT p.id, r.id, 'Allow'
FROM public.permissions p
JOIN public.roles r ON r.name = 'problem-author'
WHERE p.code IN ('problem:create:admin', 'problem:read:studio', 'problem:update:studio', 'problem:submit:admin')
ON CONFLICT DO NOTHING;
