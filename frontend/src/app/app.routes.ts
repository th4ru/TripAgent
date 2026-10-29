import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { Landing } from './features/landing/landing';

export const routes: Routes = [
  {
    // The AI assistant is the home page: opening the app drops you into the chat.
    path: '',
    pathMatch: 'full',
    redirectTo: 'chat'
  },
  {
    path: 'chat',
    title: 'AI Travel Assistant',
    canActivate: [authGuard],
    loadComponent: () => import('./features/chatbot/chatbot').then((m) => m.Chatbot)
  },
  {
    // Public marketing page (no sign-in required).
    path: 'home',
    title: 'AI Trip Planner',
    component: Landing
  },
  {
    path: 'login',
    title: 'Sign in',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login)
  },
  {
    path: 'register',
    title: 'Create account',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
