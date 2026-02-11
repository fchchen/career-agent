import { Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-score-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="score-badge" [style.background-color]="bgColor()" [style.color]="textColor()">
      {{ displayScore() }}
    </span>
  `,
  styles: `
    .score-badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 48px;
      padding: 4px 10px;
      border-radius: 16px;
      font-weight: 600;
      font-size: 13px;
    }
  `,
})
export class ScoreBadgeComponent {
  score = input.required<number>();

  displayScore = computed(() => Math.round(this.score() * 100) + '%');

  bgColor = computed(() => {
    const s = this.score();
    if (s >= 0.7) return '#1b5e20';
    if (s >= 0.4) return '#e65100';
    return '#b71c1c';
  });

  textColor = computed(() => {
    const s = this.score();
    if (s >= 0.7) return '#a5d6a7';
    if (s >= 0.4) return '#ffcc80';
    return '#ef9a9a';
  });
}
