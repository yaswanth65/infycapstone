import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from './api.service';
import { AuthService } from './auth';
import { SignupRequest } from './models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  activeTab: 'login' | 'signup' = 'login';

  constructor(public apiService: ApiService, public authService: AuthService) {}

  // Login model
  emailOrUserName = '';
  password = '';

  // Signup model
  signupModel: SignupRequest = {
    email: '',
    userName: '',
    displayName: '',
    phoneNumber: '',
    password: ''
  };

  errorMessage = '';
  successMessage = '';
  isLoading = false;

  ngOnInit() {
    if (this.authService.isAuthenticated()) {
      this.authService.redirectByRole();
    }
  }

  setTab(tab: 'login' | 'signup') {
    this.activeTab = tab;
    this.errorMessage = '';
    this.successMessage = '';
  }

  quickLogin(role: string) {
    this.activeTab = 'login';
    switch (role) {
      case 'admin':
        this.emailOrUserName = 'admin';
        this.password = 'Password@123';
        break;
      case 'manager':
        this.emailOrUserName = 'manager';
        this.password = 'Password@123';
        break;
      case 'business':
        this.emailOrUserName = 'business';
        this.password = 'Password@123';
        break;
      case 'attendee':
        this.emailOrUserName = 'attendee1';
        this.password = 'Password@123';
        break;
    }
    this.onSubmit();
  }

  onSubmit() {
    this.emailOrUserName = this.emailOrUserName.trim();
    if (!this.emailOrUserName || !this.password) {
      this.errorMessage = 'Please enter both username/email and password.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.apiService.login({ emailOrUserName: this.emailOrUserName, password: this.password }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          this.authService.login(res.data);
        } else {
          this.errorMessage = res.message || 'Login failed.';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.message || 'Invalid username/email or password.';
      }
    });
  }

  onSignupSubmit() {
    if (!this.signupModel.email || !this.signupModel.userName || !this.signupModel.displayName || !this.signupModel.password) {
      this.errorMessage = 'Please fill in all required fields (Email, Username, Display Name, Password).';
      return;
    }

    if (this.signupModel.email && !this.isValidEmail(this.signupModel.email)) {
      this.errorMessage = 'Please enter a valid email address.';
      return;
    }

    if (this.signupModel.password && !this.isStrongPassword(this.signupModel.password)) {
      this.errorMessage = 'Password does not meet strength requirements.';
      return;
    }

    if (this.signupModel.phoneNumber && !this.isValidPhone(this.signupModel.phoneNumber)) {
      this.errorMessage = 'Please enter a valid phone number (digits only, 7-15 characters).';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.apiService.signup(this.signupModel).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          this.successMessage = '🎉 Account created successfully! Logging you in...';
          setTimeout(() => {
            this.authService.login(res.data);
          }, 800);
        } else {
          this.errorMessage = res.message || 'Registration failed.';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.message || 'Registration failed. Username or email may already be in use.';
      }
    });
  }

  isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  isStrongPassword(pw: string): boolean {
    return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$/.test(pw);
  }

  isValidPhone(phone: string): boolean {
    return /^\d{7,15}$/.test(phone.replace(/[\s\-\(\)]/g, ''));
  }
}
