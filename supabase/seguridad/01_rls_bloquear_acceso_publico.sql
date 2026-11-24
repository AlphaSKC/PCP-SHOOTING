-- =====================================================================================
-- PCP Shooting · Seguridad de la base en Supabase (defensa en profundidad)
-- =====================================================================================
-- Arquitectura (CLAUDE.md §2): el navegador NUNCA consulta tablas directamente.
-- Todo pasa por la API C#, que se conecta con el rol dueño de las tablas (postgres).
--
-- Riesgo que cierra este script:
--   Supabase expone el esquema "public" por su Data API (PostgREST) a los roles
--   "anon" (visitante) y "authenticated" (cualquier usuario con sesión). Si esas tablas
--   tuvieran permisos, un cliente con su token podría, por ejemplo, editar su propio
--   "Rol" en "PerfilesUsuario" y volverse Administrador, o leer pedidos ajenos.
--
-- Qué hace:
--   1. Activa RLS en TODAS las tablas de "public" (sin políticas = nadie pasa por la Data API).
--   2. Revoca todo permiso de anon y authenticated sobre tablas, secuencias y funciones de "public".
--   3. Hace lo mismo para las tablas que se creen en el futuro (migraciones de EF Core).
--
-- Qué NO afecta:
--   - A la API C#: se conecta como "postgres" (dueño de las tablas). El dueño no está sujeto
--     a RLS mientras no se use FORCE ROW LEVEL SECURITY (este script no lo usa).
--   - A Supabase Auth ni Storage (viven en los esquemas "auth" y "storage").
--
-- ANTES DE EJECUTAR (backend):
--   * Confirma que la cadena de conexión de la API usa el usuario "postgres"
--     (o "postgres.<ref>" en el pooler). Si usa otro rol, avísame: hay que darle permisos.
--   * Ejecútalo en Supabase → SQL Editor. Es idempotente (se puede correr varias veces).
--   * Si EF Core crea tablas nuevas después, vuelve a ejecutar el bloque 1.
-- =====================================================================================

begin;

-- 1. RLS activo y sin permisos para anon/authenticated en todas las tablas existentes.
do $$
declare
  t record;
begin
  for t in
    select n.nspname as esquema, c.relname as tabla
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where n.nspname = 'public' and c.relkind in ('r', 'p')
  loop
    execute format('alter table %I.%I enable row level security', t.esquema, t.tabla);
    execute format('revoke all on table %I.%I from anon, authenticated', t.esquema, t.tabla);
  end loop;
end $$;

-- 2. Secuencias y funciones del esquema public.
revoke all on all sequences in schema public from anon, authenticated;
revoke execute on all functions in schema public from anon, authenticated;

-- 3. Objetos futuros (los que creen postgres o supabase_admin).
alter default privileges for role postgres in schema public revoke all on tables from anon, authenticated;
alter default privileges for role postgres in schema public revoke all on sequences from anon, authenticated;
alter default privileges for role postgres in schema public revoke execute on functions from anon, authenticated;

commit;

-- =====================================================================================
-- VERIFICACIÓN (deben salir 0 filas en ambas consultas)
-- =====================================================================================

-- a) Tablas de public SIN RLS:
select c.relname as tabla_sin_rls
from pg_class c join pg_namespace n on n.oid = c.relnamespace
where n.nspname = 'public' and c.relkind in ('r', 'p') and not c.relrowsecurity;

-- b) Permisos que aún tengan anon/authenticated sobre tablas de public:
select table_name, grantee, privilege_type
from information_schema.role_table_grants
where table_schema = 'public' and grantee in ('anon', 'authenticated');
