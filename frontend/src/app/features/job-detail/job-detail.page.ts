import { Component, inject, input, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { CareerService } from '../../services/career.service';
import { JobListingDto, JobStatus } from '../../models/career.models';
import { ScoreBadgeComponent } from '../../shared/components/score-badge/score-badge.component';

@Component({
  selector: 'app-job-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    ScoreBadgeComponent,
  ],
  template: `
    <div class="job-detail">
      @if (loading()) {
        <div class="center"><mat-spinner diameter="40" /></div>
      } @else if (job(); as j) {
        <div class="header">
          <div>
            <h1>{{ j.title }}</h1>
            <p class="subtitle">
              {{ j.company }} &middot; {{ j.location }}
              @if (j.salary) { &middot; {{ j.salary }} }
            </p>
          </div>
          <app-score-badge [score]="j.relevanceScore" />
        </div>

        <div class="actions">
          <button mat-raised-button color="primary" (click)="tailor()">
            <mat-icon>auto_fix_high</mat-icon> Tailor Resume
          </button>
          @if (j.applyLinks.length > 0) {
            @for (link of j.applyLinks; track link.url) {
              <a mat-stroked-button [href]="link.url" target="_blank" rel="noopener">
                <mat-icon>open_in_new</mat-icon> {{ link.title }}
              </a>
            }
          } @else {
            <a mat-stroked-button [href]="j.url" target="_blank" rel="noopener">
              <mat-icon>open_in_new</mat-icon> View Original
            </a>
          }
          <button mat-stroked-button [matMenuTriggerFor]="statusMenu">
            <mat-icon>label</mat-icon> {{ j.status }}
          </button>
          <mat-menu #statusMenu="matMenu">
            <button mat-menu-item (click)="setStatus('Applied')">Applied</button>
            <button mat-menu-item (click)="setStatus('Dismissed')">Dismissed</button>
            <button mat-menu-item (click)="setStatus('New')">New</button>
          </mat-menu>
        </div>

        <mat-divider />

        <div class="skills-section">
          <h3>Skill Match</h3>
          <div class="skill-chips">
            @for (skill of j.matchedSkills; track skill) {
              <mat-chip class="matched">{{ skill }}</mat-chip>
            }
            @for (skill of j.missingSkills; track skill) {
              <mat-chip class="missing">{{ skill }}</mat-chip>
            }
          </div>
        </div>

        <mat-divider />

        <div class="description">
          <h3>Job Description</h3>
          <div class="desc-text">{{ j.description }}</div>
        </div>

        <div class="meta-info">
          <span>Source: {{ j.source }}</span>
          <span>Posted: {{ j.postedAt | date: 'mediumDate' }}</span>
          <span>Fetched: {{ j.fetchedAt | date: 'medium' }}</span>
        </div>
      }
    </div>
  `,
  styles: `
    .job-detail {
      padding: 24px;
      max-width: 900px;
      margin: 0 auto;
    }
    .center { display: flex; justify-content: center; padding: 48px; }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 16px;
    }
    h1 { margin: 0; }
    .subtitle { color: var(--mat-sys-on-surface-variant); margin-top: 4px; }
    .actions {
      display: flex;
      gap: 12px;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }
    .skills-section { margin: 20px 0; }
    .skill-chips {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      margin-top: 8px;
    }
    .matched {
      --mdc-chip-elevated-container-color: #1b5e20;
      --mdc-chip-label-text-color: #a5d6a7;
    }
    .missing {
      --mdc-chip-elevated-container-color: #b71c1c;
      --mdc-chip-label-text-color: #ef9a9a;
    }
    .description { margin: 20px 0; }
    .desc-text {
      white-space: pre-wrap;
      line-height: 1.6;
      font-size: 14px;
    }
    .meta-info {
      display: flex;
      gap: 24px;
      color: var(--mat-sys-on-surface-variant);
      font-size: 12px;
      margin-top: 24px;
    }
  `,
})
export class JobDetailPage implements OnInit {
  id = input.required<number>();

  private careerService = inject(CareerService);
  private router = inject(Router);

  loading = signal(true);
  job = signal<JobListingDto | null>(null);

  ngOnInit() {
    this.careerService.getJobById(this.id()).subscribe({
      next: (j) => {
        this.job.set(j);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  tailor() {
    this.router.navigate(['/tailor', this.id()]);
  }

  setStatus(status: JobStatus) {
    this.careerService.updateJobStatus(this.id(), status).subscribe(() => {
      const current = this.job();
      if (current) this.job.set({ ...current, status });
    });
  }
}
