import { Component, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-skill-chip',
  standalone: true,
  imports: [MatChipsModule],
  template: `
    <mat-chip [class]="matched() ? 'matched' : 'missing'" [highlighted]="matched()">
      {{ skill() }}
    </mat-chip>
  `,
  styles: `
    .matched {
      --mdc-chip-elevated-container-color: #e8f5e9;
      --mdc-chip-label-text-color: #2e7d32;
    }
    .missing {
      --mdc-chip-elevated-container-color: #ffebee;
      --mdc-chip-label-text-color: #c62828;
    }
  `,
})
export class SkillChipComponent {
  skill = input.required<string>();
  matched = input(true);
}
