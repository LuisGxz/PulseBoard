import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { parseApiError } from '../../core/api-error';
import { AuthService } from '../../core/auth.service';
import { LanguageService } from '../../core/language.service';

interface Demo { email: string; password: string; role: 'owner' | 'editor' | 'viewer'; }
const DEMOS: Demo[] = [
  { email: 'admin@pulseboard.io', password: 'Admin123!', role: 'owner' },
  { email: 'editor@pulseboard.io', password: 'Editor123!', role: 'editor' },
  { email: 'viewer@pulseboard.io', password: 'Viewer123!', role: 'viewer' },
];

@Component({
  selector: 'pb-login',
  standalone: true,
  imports: [FormsModule, RouterLink, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-dvh grid place-items-center px-4 py-10">
      <div class="w-full max-w-sm">
        <div class="flex items-center gap-2.5 justify-center mb-8">
          <span class="w-9 h-9 rounded-xl grid place-items-center" style="background:linear-gradient(135deg,#22d3ee,#8b5cf6)">
            <lucide-icon name="bar-chart-3" class="w-5 h-5 text-gr-950"></lucide-icon>
          </span>
          <div class="leading-tight">
            <p class="font-semibold text-white">{{ t().brand }}</p>
            <p class="text-[11px] text-gr-300 font-mono">{{ t().brandSub }}</p>
          </div>
        </div>

        <div class="widget p-6">
          <h1 class="text-lg font-semibold text-white">{{ t().auth.title }}</h1>
          <p class="text-sm text-gr-300 mt-1 mb-5">{{ t().auth.subtitle }}</p>

          <form (ngSubmit)="submit()" class="space-y-4">
            <div>
              <label class="label" for="email">{{ t().auth.email }}</label>
              <input id="email" name="email" type="email" class="input" [(ngModel)]="email" autocomplete="username" required>
            </div>
            <div>
              <label class="label" for="password">{{ t().auth.password }}</label>
              <input id="password" name="password" type="password" class="input" [(ngModel)]="password" autocomplete="current-password" required>
            </div>

            @if (error()) { <p class="field-error">{{ error() }}</p> }

            <button type="submit" class="btn-primary w-full" [disabled]="loading()">
              @if (loading()) {
                <lucide-icon name="loader-2" class="w-4 h-4 animate-spin"></lucide-icon>{{ t().auth.signingIn }}
              } @else { {{ t().auth.login }} }
            </button>
          </form>

          <p class="text-sm text-gr-300 mt-4">
            {{ t().auth.noAccount }}
            <a routerLink="/register" class="text-cy-400 hover:text-cy-300 font-medium">{{ t().auth.createAccount }}</a>
          </p>
        </div>

        <div class="mt-4 widget p-4">
          <p class="text-xs font-semibold text-gr-200 mb-3">{{ t().auth.demoAccounts }}</p>
          <div class="space-y-2">
            @for (d of demos; track d.email) {
              <button (click)="useDemo(d)"
                class="w-full flex items-center justify-between gap-2 rounded-lg border border-gr-700 px-3 py-2
                       hover:border-cy-400 transition-colors text-left">
                <span class="min-w-0">
                  <span class="block text-xs font-medium text-gr-100 truncate">{{ d.email }}</span>
                  <span class="block text-[11px] text-gr-400">{{ demoLabel(d.role) }}</span>
                </span>
                <span class="chip shrink-0">{{ t().auth.useAccount }}</span>
              </button>
            }
          </div>
        </div>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly lang = inject(LanguageService);
  protected readonly t = this.lang.t;
  protected readonly demos = DEMOS;

  protected email = '';
  protected password = '';
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected demoLabel(role: Demo['role']): string {
    const a = this.t().auth;
    return role === 'owner' ? a.ownerDemo : role === 'editor' ? a.editorDemo : a.viewerDemo;
  }

  protected useDemo(d: Demo): void {
    this.email = d.email;
    this.password = d.password;
    this.submit();
  }

  protected async submit(): Promise<void> {
    if (this.loading() || !this.email || !this.password) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.auth.login(this.email.trim(), this.password);
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboards';
      this.router.navigateByUrl(returnUrl);
    } catch (err) {
      this.error.set(parseApiError(err, 'Sign in failed.', this.lang.lang())[0]);
    } finally {
      this.loading.set(false);
    }
  }
}
