import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrls: ['../auth-shared.scss']
})
export class Register {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.formBuilder.nonNullable.group({
    firstName: [''],
    lastName: [''],
    userName: [
      '',
      [Validators.required, Validators.minLength(3), Validators.maxLength(40), Validators.pattern(/^[a-zA-Z0-9._-]+$/)]
    ],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    confirmPassword: ['', [Validators.required]]
  });

  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    if (this.submitting()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    if (value.password !== value.confirmPassword) {
      this.error.set('The passwords do not match.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.auth
      .register({
        userName: value.userName.trim(),
        email: value.email.trim(),
        password: value.password,
        firstName: value.firstName.trim(),
        lastName: value.lastName.trim()
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          void this.router.navigateByUrl('/chat');
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err?.error?.error ?? 'Could not create your account. Please try again.');
        }
      });
  }

  isInvalid(field: 'userName' | 'email' | 'password' | 'confirmPassword'): boolean {
    const control = this.form.controls[field];
    return control.invalid && (control.touched || control.dirty);
  }
}
