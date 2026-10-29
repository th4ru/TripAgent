import { Component } from '@angular/core';
import { NeuralBackground } from '../neural-background/neural-background';

/**
 * The "agentic" backdrop shared by the landing page and the assistant:
 * animated neural network + drifting glows + faint grid + bottom fade.
 * Purely decorative — the host ignores pointer events.
 */
@Component({
  selector: 'app-agentic-backdrop',
  imports: [NeuralBackground],
  templateUrl: './agentic-backdrop.html',
  styleUrl: './agentic-backdrop.scss'
})
export class AgenticBackdrop {}
