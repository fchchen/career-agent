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
      --mdc-chip-elevated-container-color: #1b5e20;
      --mdc-chip-label-text-color: #a5d6a7;
    }
    .missing {
      --mdc-chip-elevated-container-color: #b71c1c;
      --mdc-chip-label-text-color: #ef9a9a;
    }
  `,
})
export class SkillChipComponent {
  skill = input.required<string>();
  matched = input(true);
}
