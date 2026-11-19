/*
 * Punto único de autenticación para la app.
 * - Con VITE_SUPABASE_URL y VITE_SUPABASE_PUBLISHABLE_KEY válidas → Supabase + API (authSupabase.ts).
 * - Sin credenciales → modo demostración (authDemo.ts).
 */
import { isSupabaseConfigured } from '@/lib/supabase';
import * as demo from './authDemo';
import * as real from './authSupabase';

export const AUTH_MODE: 'supabase' | 'demo' = isSupabaseConfigured ? 'supabase' : 'demo';
export const GOOGLE_ENABLED = AUTH_MODE === 'supabase' && import.meta.env.VITE_AUTH_GOOGLE === 'true';

const impl = AUTH_MODE === 'supabase' ? real : demo;

export const getSession = impl.getSession;
export const signIn = impl.signIn;
export const signUp = impl.signUp;
export const signOut = impl.signOut;
export const resetPassword = impl.resetPassword;
export const updatePassword = impl.updatePassword;
export const updateProfile = impl.updateProfile;
export const resendConfirmation = impl.resendConfirmation;
export const activateMembership = impl.activateMembership as typeof demo.activateMembership;
export const signInWithGoogle = (next?: string) => (AUTH_MODE === 'supabase' ? real.signInWithGoogle(next) : demo.signInWithGoogle());
