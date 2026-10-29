export interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  r: number;
  color: string;
  glow: string;
}

export const PALETTE = [
  { color: 'rgba(96, 165, 250, 0.95)', glow: 'rgba(96, 165, 250, 0.12)' },
  { color: 'rgba(34, 211, 238, 0.9)', glow: 'rgba(34, 211, 238, 0.12)' },
  { color: 'rgba(167, 139, 250, 0.9)', glow: 'rgba(167, 139, 250, 0.12)' }
];
