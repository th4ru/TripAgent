import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AgenticBackdrop } from '../../shared/components/agentic-backdrop/agentic-backdrop';

@Component({
  selector: 'app-landing',
  imports: [RouterLink, AgenticBackdrop],
  templateUrl: './landing.html',
  styleUrl: './landing.scss'
})
export class Landing {
  readonly auth = inject(AuthService);
}
