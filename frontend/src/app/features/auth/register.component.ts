import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { parseApiError } from '../../core/api-error';
import { AuthService } from '../../core/auth.service';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'pb-register',
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
          <h1 class="text-lg font-semibold text-white">{{ t().auth.register }}</h1>
          <p class="text-sm text-gr-300 mt-1 mb-5">{{ t().auth.subtitle }}</p>

          <form (ngSubmit)="submit()" class="space-y-4">
            <div>
              <label class="label" for="name">{{ t().auth.displayName }}</label>
              <input id="name" name="name" class="input" [(ngModel)]="displayName" autocomplete="name" required>
            </div>
            <div>
              <label class="label" for="email">{{ t().auth.email }}</label>
              <input id="email" name="email" type="email" class="input" [(ngModel)]="email" autocomplete="username" required>
            </div>
            <div>
              <label class="label" for="password">{{ t().auth.password }}</label>
              <input id="password" name="password" type="password" class="input" [(ngModel)]="password" autocomplete="new-password" required>
              <p class="text-[11px] text-gr-400 mt-1">Min 8 chars · upper, lower & a digit.</p>
            </div>

            @if (error()) { <p class="field-error">{{ error() }}</p> }

            <button type="submit" class="btn-primary w-full" [disabled]="loading()">
              @if (loading()) {
                <lucide-icon name="loader-2" class="w-4 h-4 animate-spin"></lucide-icon>{{ t().auth.signingIn }}
              } @else { {{ t().auth.createAccount }} }
            </button>
          </form>

          <p class="text-sm text-gr-300 mt-4">
            {{ t().auth.haveAccount }}
            <a routerLink="/login" class="text-cy-400 hover:text-cy-300 font-medium">{{ t().auth.login }}</a>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly lang = inject(LanguageService);
  protected readonly t = this.lang.t;

  protected displayName = '';
  protected email = '';
  protected password = '';
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async submit(): Promise<void> {
    if (this.loading() || !this.email || !this.password || !this.displayName) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.auth.register(this.email.trim(), this.password, this.displayName.trim());
      this.router.navigateByUrl('/dashboards');
    } catch (err) {
      this.error.set(parseApiError(err, 'Registration failed.', this.lang.lang())[0]);
    } finally {
      this.loading.set(false);
    }
  }
}
