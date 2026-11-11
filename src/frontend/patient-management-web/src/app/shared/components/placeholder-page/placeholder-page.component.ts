import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { TranslatePipe } from '../../pipes/translate.pipe';

/** Stand-in for a feature screen that Sprint 1 only reserves. Route data supplies the keys. */
@Component({
  selector: 'app-placeholder-page',
  imports: [TranslatePipe],
  templateUrl: './placeholder-page.component.html',
  styleUrl: './placeholder-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlaceholderPageComponent {
  readonly titleKey = input.required<string>();
  readonly descriptionKey = input.required<string>();
}
