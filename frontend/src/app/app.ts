import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  readonly auth = inject(AuthService);

  /** Members go straight to the assistant; visitors land on the marketing page. */
  readonly brandLink = computed(() => (this.auth.isAuthenticated() ? '/chat' : '/home'));

  signOut(): void {
    this.auth.signOut('/login');
  }
}
