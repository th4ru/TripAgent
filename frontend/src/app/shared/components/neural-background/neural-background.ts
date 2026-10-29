import { AfterViewInit, Component, ElementRef, NgZone, OnDestroy, inject, viewChild } from '@angular/core';
import { PALETTE, Particle } from './neural-background.types';

/**
 * Animated "agentic" network: drifting nodes connected by fading links, with a
 * pointer that pulls nearby connections into focus. Pure canvas, no libraries.
 */
@Component({
  selector: 'app-neural-background',
  templateUrl: './neural-background.html',
  styleUrl: './neural-background.scss'
})
export class NeuralBackground implements AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly zone = inject(NgZone);

  private particles: Particle[] = [];
  private width = 0;
  private height = 0;
  private frameId = 0;
  private running = false;
  private resizeObserver?: ResizeObserver;

  private pointer = { x: -10_000, y: -10_000, active: false };

  ngAfterViewInit(): void {
    const canvas = this.canvasRef().nativeElement;
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    this.zone.runOutsideAngular(() => {
      this.resizeObserver = new ResizeObserver(() => this.resize(canvas));
      this.resizeObserver.observe(canvas.parentElement ?? canvas);
      this.resize(canvas);

      window.addEventListener('pointermove', this.onPointerMove, { passive: true });

      if (reducedMotion) {
        this.running = false;
        this.draw(canvas);
      } else {
        this.running = true;
        this.frameId = requestAnimationFrame(this.frame);
      }
    });
  }

  ngOnDestroy(): void {
    this.running = false;
    cancelAnimationFrame(this.frameId);
    this.resizeObserver?.disconnect();
    window.removeEventListener('pointermove', this.onPointerMove);
  }

  private readonly frame = (): void => {
    if (!this.running) {
      return;
    }

    const canvas = this.canvasRef().nativeElement;
    this.update();
    this.draw(canvas);
    this.frameId = requestAnimationFrame(this.frame);
  };

  private readonly onPointerMove = (event: PointerEvent): void => {
    const canvas = this.canvasRef().nativeElement;
    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    this.pointer = {
      x,
      y,
      active: x >= 0 && y >= 0 && x <= rect.width && y <= rect.height
    };
  };

  private resize(canvas: HTMLCanvasElement): void {
    const host = canvas.parentElement;
    const rect = host?.getBoundingClientRect() ?? canvas.getBoundingClientRect();

    if (rect.width < 1 || rect.height < 1) {
      return;
    }

    this.width = rect.width;
    this.height = rect.height;

    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.round(this.width * dpr);
    canvas.height = Math.round(this.height * dpr);

    const context = canvas.getContext('2d');
    if (context) {
      context.setTransform(dpr, 0, 0, dpr, 0, 0);
    }

    this.spawn();

    if (!this.running) {
      this.draw(canvas);
    }
  }

  private spawn(): void {
    const target = Math.min(95, Math.max(26, Math.round((this.width * this.height) / 19_000)));
    this.particles = Array.from({ length: target }, () => this.createParticle());
  }

  private createParticle(): Particle {
    const palette = PALETTE[Math.floor(Math.random() * PALETTE.length)];

    return {
      x: Math.random() * this.width,
      y: Math.random() * this.height,
      vx: (Math.random() - 0.5) * 0.28,
      vy: (Math.random() - 0.5) * 0.28,
      r: 0.9 + Math.random() * 1.6,
      color: palette.color,
      glow: palette.glow
    };
  }

  private update(): void {
    const margin = 40;

    for (const particle of this.particles) {
      particle.x += particle.vx;
      particle.y += particle.vy;

      if (particle.x < -margin) particle.x = this.width + margin;
      if (particle.x > this.width + margin) particle.x = -margin;
      if (particle.y < -margin) particle.y = this.height + margin;
      if (particle.y > this.height + margin) particle.y = -margin;

      if (!this.pointer.active) {
        continue;
      }

      const dx = particle.x - this.pointer.x;
      const dy = particle.y - this.pointer.y;
      const distanceSquared = dx * dx + dy * dy;
      const radius = 170;

      if (distanceSquared > 0.01 && distanceSquared < radius * radius) {
        const distance = Math.sqrt(distanceSquared);
        const push = ((radius - distance) / radius) * 0.5;
        particle.x += (dx / distance) * push;
        particle.y += (dy / distance) * push;
      }
    }
  }

  private draw(canvas: HTMLCanvasElement): void {
    const context = canvas.getContext('2d');
    if (!context) {
      return;
    }

    context.clearRect(0, 0, this.width, this.height);

    const linkDistance = Math.min(165, Math.max(105, this.width / 11));

    for (let i = 0; i < this.particles.length; i++) {
      const a = this.particles[i];

      for (let j = i + 1; j < this.particles.length; j++) {
        const b = this.particles[j];
        const dx = a.x - b.x;
        const dy = a.y - b.y;
        const distance = Math.sqrt(dx * dx + dy * dy);

        if (distance >= linkDistance) {
          continue;
        }

        const alpha = (1 - distance / linkDistance) * 0.3;
        context.strokeStyle = `rgba(120, 168, 255, ${alpha.toFixed(3)})`;
        context.lineWidth = 1;
        context.beginPath();
        context.moveTo(a.x, a.y);
        context.lineTo(b.x, b.y);
        context.stroke();
      }
    }

    if (this.pointer.active) {
      const reach = 220;

      for (const particle of this.particles) {
        const dx = particle.x - this.pointer.x;
        const dy = particle.y - this.pointer.y;
        const distance = Math.sqrt(dx * dx + dy * dy);

        if (distance >= reach) {
          continue;
        }

        const alpha = (1 - distance / reach) * 0.55;
        context.strokeStyle = `rgba(34, 211, 238, ${alpha.toFixed(3)})`;
        context.lineWidth = 1.1;
        context.beginPath();
        context.moveTo(particle.x, particle.y);
        context.lineTo(this.pointer.x, this.pointer.y);
        context.stroke();
      }

      const halo = context.createRadialGradient(
        this.pointer.x,
        this.pointer.y,
        0,
        this.pointer.x,
        this.pointer.y,
        120
      );
      halo.addColorStop(0, 'rgba(34, 211, 238, 0.16)');
      halo.addColorStop(1, 'rgba(34, 211, 238, 0)');
      context.fillStyle = halo;
      context.beginPath();
      context.arc(this.pointer.x, this.pointer.y, 120, 0, Math.PI * 2);
      context.fill();
    }

    for (const particle of this.particles) {
      context.fillStyle = particle.glow;
      context.beginPath();
      context.arc(particle.x, particle.y, particle.r * 3.4, 0, Math.PI * 2);
      context.fill();

      context.fillStyle = particle.color;
      context.beginPath();
      context.arc(particle.x, particle.y, particle.r, 0, Math.PI * 2);
      context.fill();
    }
  }
}
