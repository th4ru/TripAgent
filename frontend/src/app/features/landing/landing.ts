import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NeuralBackground } from './neural-background/neural-background';

@Component({
  selector: 'app-landing',
  imports: [RouterLink, NeuralBackground],
  templateUrl: './landing.html',
  styleUrl: './landing.scss'
})
export class Landing {
  readonly auth = inject(AuthService);
}
