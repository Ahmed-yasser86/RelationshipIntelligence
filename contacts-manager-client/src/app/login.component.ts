import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-card">
      <h2>Login</h2>
      <form (ngSubmit)="submit()">
        <label>Email</label>
        <input type="email" [(ngModel)]="email" name="email" required />

        <label>Password</label>
        <input type="password" [(ngModel)]="password" name="password" required />

        <button type="submit">Sign In</button>
      </form>
      <p class="message" *ngIf="message">{{ message }}</p>
    </div>
  `,
  styles: [
    `
      .login-card { max-width: 360px; margin: 80px auto; padding: 24px; border-radius: 12px; box-shadow: 0 10px 30px rgba(0,0,0,.12); font-family: Arial, sans-serif; }
      h2 { margin-top: 0; }
      form { display: flex; flex-direction: column; gap: 10px; }
      input { padding: 10px; border: 1px solid #ccc; border-radius: 8px; }
      button { padding: 10px; border: none; border-radius: 8px; background: #2563eb; color: white; cursor: pointer; }
      .message { margin-top: 12px; color: #b91c1c; }
    `
  ]
})
export class LoginComponent {
  email = '';
  password = '';
  message = '';

  constructor(private authService: AuthService) {}

  submit(): void {
    this.message = '';
    this.authService.login(this.email, this.password).subscribe({
      next: (res) => {
        localStorage.setItem('token', res.token);
        localStorage.setItem('user', JSON.stringify({ name: res.personeName, email: res.personeEmail }));
        this.message = 'Login successful';
      },
      error: () => {
        this.message = 'Invalid email or password';
      }
    });
  }
}
