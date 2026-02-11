import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CareerService } from '../../services/career.service';
import { DashboardResponse } from '../../models/career.models';
import { JobCardComponent } from '../../shared/components/job-card/job-card.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    JobCardComponent,
  ],
  template: `
    <div class="dashboard">
      <h1>Dashboard</h1>

      @if (loading()) {
        <div class="center"><mat-spinner diameter="40" /></div>
      } @else if (data(); as d) {
        <div class="stats-grid">
          <mat-card>
            <mat-card-content>
              <div class="stat-value">{{ d.stats.totalJobs }}</div>
              <div class="stat-label">Total Jobs</div>
            </mat-card-content>
          </mat-card>
          <mat-card>
            <mat-card-content>
              <div class="stat-value new">{{ d.stats.newJobs }}</div>
              <div class="stat-label">New</div>
            </mat-card-content>
          </mat-card>
          <mat-card>
            <mat-card-content>
              <div class="stat-value applied">{{ d.stats.appliedJobs }}</div>
              <div class="stat-label">Applied</div>
            </mat-card-content>
          </mat-card>
          <mat-card>
            <mat-card-content>
              <div class="stat-value">{{ (d.stats.averageScore * 100).toFixed(0) }}%</div>
              <div class="stat-label">Avg Score</div>
            </mat-card-content>
          </mat-card>
        </div>

        <section>
          <div class="section-header">
            <h2>Top Scoring Jobs</h2>
            <a mat-button routerLink="/jobs" color="primary">View All</a>
          </div>
          <div class="job-list">
            @for (job of d.topJobs; track job.id) {
              <app-job-card [job]="job" />
            }
            @empty {
              <p class="empty">No jobs found. Run a search to get started.</p>
            }
          </div>
        </section>

        <section>
          <div class="section-header">
            <h2>Recently Posted</h2>
          </div>
          <div class="job-list">
            @for (job of d.recentJobs; track job.id) {
              <app-job-card [job]="job" />
            }
          </div>
        </section>
      }
    </div>
  `,
  styles: `
    .dashboard {
      padding: 24px;
      max-width: 1000px;
      margin: 0 auto;
    }
    h1 { margin-bottom: 24px; }
    .center { display: flex; justify-content: center; padding: 48px; }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;
      margin-bottom: 32px;
    }
    .stat-value {
      font-size: 32px;
      font-weight: 700;
      text-align: center;
    }
    .stat-value.new { color: #64b5f6; }
    .stat-value.applied { color: #81c784; }
    .stat-label {
      text-align: center;
      color: var(--mat-sys-on-surface-variant);
      font-size: 14px;
      margin-top: 4px;
    }
    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }
    h2 { margin: 0; }
    section { margin-bottom: 32px; }
    .job-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .empty { color: var(--mat-sys-on-surface-variant); text-align: center; padding: 24px; }
    @media (max-width: 600px) {
      .stats-grid { grid-template-columns: repeat(2, 1fr); }
    }
  `,
})
export class DashboardPage implements OnInit {
  private careerService = inject(CareerService);
  loading = signal(true);
  data = signal<DashboardResponse | null>(null);

  ngOnInit() {
    this.careerService.getDashboard().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
