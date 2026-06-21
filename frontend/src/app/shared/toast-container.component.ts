import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'pb-toasts',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2 w-[min(92vw,22rem)]">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="flex items-start gap-3 rounded-lg border px-3.5 py-3 text-sm bg-gr-850 toast-in"
             [class.border-li-500]="toast.kind === 'success'"
             [class.border-rose-400]="toast.kind === 'error'"
             [class.border-gr-600]="toast.kind === 'info'">
          <lucide-icon
            [name]="toast.kind === 'success' ? 'check' : toast.kind === 'error' ? 'alert-triangle' : 'info'"
            class="w-4 h-4 mt-0.5 shrink-0"
            [class.text-li-400]="toast.kind === 'success'"
            [class.text-rose-400]="toast.kind === 'error'"
            [class.text-cy-400]="toast.kind === 'info'"></lucide-icon>
          <span class="flex-1 text-gr-100">{{ toast.message }}</span>
          <button class="text-gr-400 hover:text-white transition-colors" (click)="toasts.dismiss(toast.id)">
            <lucide-icon name="x" class="w-4 h-4"></lucide-icon>
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-in { animation: toast-in .2s ease-out; }
    @keyframes toast-in { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: none; } }
  `],
})
export class ToastContainerComponent {
  protected readonly toasts = inject(ToastService);
}
