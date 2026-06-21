import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../core/auth.service';
import { LanguageService } from '../core/language.service';

@Component({
  selector: 'pb-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="sticky top-0 z-40 flex items-center justify-between gap-4 px-4 sm:px-6 py-3
                   bg-gr-950/90 backdrop-blur border-b border-gr-700">
      <a routerLink="/dashboards" class="flex items-center gap-2.5 shrink-0">
        <span class="w-7 h-7 rounded-lg grid place-items-center"
              style="background:linear-gradient(135deg,#22d3ee,#8b5cf6)">
          <lucide-icon name="bar-chart-3" class="w-4 h-4 text-gr-950"></lucide-icon>
        </span>
        <span class="font-semibold text-white tracking-tight">{{ t().brand }}</span>
      </a>

      @if (auth.isAuthenticated()) {
        <nav class="hidden sm:flex items-center gap-1 text-sm">
          <a routerLink="/dashboards" routerLinkActive="text-white bg-gr-800"
             class="px-3 py-1.5 rounded-lg text-gr-200 hover:text-white hover:bg-gr-800 transition-colors">
            {{ t().nav.dashboards }}
          </a>
          <a routerLink="/datasets" routerLinkActive="text-white bg-gr-800"
             class="px-3 py-1.5 rounded-lg text-gr-200 hover:text-white hover:bg-gr-800 transition-colors">
            {{ t().nav.datasets }}
          </a>
          <a routerLink="/about" routerLinkActive="text-white bg-gr-800"
             class="px-3 py-1.5 rounded-lg text-gr-200 hover:text-white hover:bg-gr-800 transition-colors">
            {{ t().nav.about }}
          </a>
        </nav>
      }

      <div class="flex items-center gap-2 ml-auto">
        <div class="flex rounded-full border border-gr-600 overflow-hidden text-xs font-semibold">
          <button (click)="lang.set('en')" [class.active-lang]="!lang.isEs()"
                  class="px-2.5 py-1 transition-colors"
                  [class.text-gr-300]="lang.isEs()">EN</button>
          <button (click)="lang.set('es')" [class.active-lang]="lang.isEs()"
                  class="px-2.5 py-1 transition-colors"
                  [class.text-gr-300]="!lang.isEs()">ES</button>
        </div>

        @if (auth.isAuthenticated(); as authed) {
          <div class="hidden md:flex items-center gap-2 pl-2 border-l border-gr-700">
            <span class="text-xs text-gr-300 max-w-[10rem] truncate">{{ auth.user()?.displayName }}</span>
            <button class="btn-ghost" (click)="signOut()" [attr.aria-label]="t().nav.signOut">
              <lucide-icon name="log-out" class="w-4 h-4"></lucide-icon>
            </button>
          </div>
        }
      </div>
    </header>
  `,
  styles: [`
    .active-lang { background: var(--color-cy-400); color: var(--color-gr-950); }
  `],
})
export class HeaderComponent {
  protected readonly auth = inject(AuthService);
  protected readonly lang = inject(LanguageService);
  private readonly router = inject(Router);
  protected readonly t = this.lang.t;

  async signOut(): Promise<void> {
    await this.auth.logout();
    this.router.navigate(['/login']);
  }
}
