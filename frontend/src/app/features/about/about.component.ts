import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth.service';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'pb-about',
  standalone: true,
  imports: [LucideAngularModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="max-w-5xl mx-auto px-4 sm:px-6 py-12">
      <!-- hero -->
      <div class="flex items-center gap-3 mb-4">
        <span class="w-10 h-10 rounded-xl grid place-items-center" style="background:linear-gradient(135deg,#22d3ee,#8b5cf6)">
          <lucide-icon name="bar-chart-3" class="w-5 h-5 text-gr-950"></lucide-icon>
        </span>
        <h1 class="text-2xl font-semibold text-white">{{ t().about.title }}</h1>
      </div>
      <p class="text-gr-300 leading-relaxed max-w-3xl">{{ t().about.lead }}</p>

      <!-- how it works -->
      <h2 class="text-xs font-mono uppercase tracking-widest text-cy-400 mt-12 mb-4">{{ t().about.howTitle }}</h2>
      <div class="grid md:grid-cols-3 gap-4">
        <div class="widget p-5">
          <lucide-icon name="database" class="w-5 h-5 text-cy-400 mb-3"></lucide-icon>
          <h3 class="font-semibold text-white">{{ t().about.ingestTitle }}</h3>
          <p class="text-sm text-gr-300 mt-1.5 leading-relaxed">{{ t().about.ingestBody }}</p>
          <p class="text-[11px] font-mono text-gr-500 mt-3">Python · FastAPI · pandas</p>
        </div>
        <div class="widget p-5">
          <lucide-icon name="settings-2" class="w-5 h-5 text-vi-400 mb-3"></lucide-icon>
          <h3 class="font-semibold text-white">{{ t().about.orchestrateTitle }}</h3>
          <p class="text-sm text-gr-300 mt-1.5 leading-relaxed">{{ t().about.orchestrateBody }}</p>
          <p class="text-[11px] font-mono text-gr-500 mt-3">.NET 9 · EF Core · PostgreSQL</p>
        </div>
        <div class="widget p-5">
          <lucide-icon name="line-chart" class="w-5 h-5 text-li-400 mb-3"></lucide-icon>
          <h3 class="font-semibold text-white">{{ t().about.visualizeTitle }}</h3>
          <p class="text-sm text-gr-300 mt-1.5 leading-relaxed">{{ t().about.visualizeBody }}</p>
          <p class="text-[11px] font-mono text-gr-500 mt-3">Angular 20 · Tailwind · ApexCharts</p>
        </div>
      </div>

      <!-- highlights -->
      <h2 class="text-xs font-mono uppercase tracking-widest text-cy-400 mt-12 mb-4">{{ t().about.featuresTitle }}</h2>
      <ul class="grid sm:grid-cols-2 gap-3">
        @for (f of t().about.features; track f) {
          <li class="flex items-start gap-2.5 widget p-4">
            <lucide-icon name="check" class="w-4 h-4 text-li-400 mt-0.5 shrink-0"></lucide-icon>
            <span class="text-sm text-gr-200 leading-relaxed">{{ f }}</span>
          </li>
        }
      </ul>

      <!-- stack -->
      <h2 class="text-xs font-mono uppercase tracking-widest text-cy-400 mt-12 mb-4">{{ t().about.stackTitle }}</h2>
      <div class="flex flex-wrap gap-2">
        @for (tag of stack; track tag) { <span class="chip">{{ tag }}</span> }
      </div>

      <!-- demo cta -->
      <div class="widget p-6 mt-12 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h3 class="font-semibold text-white">{{ t().about.demoTitle }}</h3>
          <p class="text-sm text-gr-300 mt-1">{{ t().about.demoBody }}</p>
        </div>
        @if (auth.isAuthenticated()) {
          <a routerLink="/dashboards" class="btn-primary">{{ t().nav.dashboards }}
            <lucide-icon name="chevron-right" class="w-4 h-4"></lucide-icon></a>
        } @else {
          <a routerLink="/login" class="btn-primary">{{ t().nav.signIn }}</a>
        }
      </div>
    </section>
  `,
})
export class AboutComponent {
  private readonly lang = inject(LanguageService);
  protected readonly auth = inject(AuthService);
  protected readonly t = this.lang.t;
  protected readonly stack = [
    'Angular 20', 'TypeScript', 'Tailwind v4', 'ApexCharts', 'Angular CDK',
    '.NET 9', 'EF Core', 'MediatR', 'FluentValidation',
    'Python', 'FastAPI', 'pandas', 'PostgreSQL', 'Docker', 'JWT · RBAC',
  ];
}
