import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { HeaderComponent } from './layout/header.component';
import { ToastContainerComponent } from './shared/toast-container.component';
import { TourComponent } from './shared/tour.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, ToastContainerComponent, TourComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-dvh flex flex-col">
      @if (auth.isAuthenticated()) { <pb-header /> }
      <main class="flex-1"><router-outlet /></main>
    </div>
    <pb-toasts />
    <pb-tour />
  `,
})
export class App {
  protected readonly auth = inject(AuthService);
}
