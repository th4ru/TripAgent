import { Routes } from '@angular/router';
import { Landing } from './features/landing/landing';

export const routes: Routes = [
  {
    path: '',
    component: Landing
  },
  {
    path: 'chat',
    loadComponent: () => import('./features/chatbot/chatbot').then((m) => m.Chatbot)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
