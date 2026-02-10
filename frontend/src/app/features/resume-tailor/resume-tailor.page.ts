import { Component, inject, input, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDividerModule } from '@angular/material/divider';
import { CareerService } from '../../services/career.service';
import { JobListingDto, TailoredDocumentDto } from '../../models/career.models';
import { ScoreBadgeComponent } from '../../shared/components/score-badge/score-badge.component';

@Component({
  selector: 'app-resume-tailor',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatDividerModule,
    ScoreBadgeComponent,
  ],
  template: `
    <div class="tailor-page">
      @if (loadingJob()) {
        <div class="center"><mat-spinner diameter="40" /></div>
      } @else if (job(); as j) {
        <h1>Tailor Resume</h1>
        <p class="subtitle">
          {{ j.title }} at {{ j.company }}
          <app-score-badge [score]="j.relevanceScore" />
        </p>

        @if (!tailored() && !tailoring()) {
          <button mat-raised-button color="primary" (click)="startTailoring()">
            <mat-icon>auto_fix_high</mat-icon> Generate Tailored Resume & Cover Letter
          </button>
        }

        @if (tailoring()) {
          <div class="tailoring-status">
            <mat-spinner diameter="32" />
            <span>Claude is tailoring your resume... This may take 15-30 seconds.</span>
          </div>
        }

        @if (tailored(); as doc) {
          <div class="split-view">
            <div class="left-panel">
              <h3>Job Description</h3>
              <div class="content-box">{{ j.description }}</div>
            </div>
            <div class="right-panel">
              <mat-tab-group>
                <mat-tab label="Tailored Resume">
                  <div class="content-box markdown">{{ doc.tailoredResumeMarkdown }}</div>
                </mat-tab>
                <mat-tab label="Cover Letter">
                  <div class="content-box markdown">{{ doc.coverLetterMarkdown }}</div>
                </mat-tab>
              </mat-tab-group>
              <div class="download-actions">
                <a
                  mat-raised-button
                  color="accent"
                  [href]="pdfUrl()"
                  target="_blank"
                >
                  <mat-icon>download</mat-icon> Download PDF
                </a>
              </div>
            </div>
          </div>
        }

        @if (error()) {
          <mat-card class="error-card">
            <mat-card-content>
              <mat-icon>error</mat-icon> {{ error() }}
            </mat-card-content>
          </mat-card>
        }
      }
    </div>
  `,
  styles: `
    .tailor-page {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }
    .center { display: flex; justify-content: center; padding: 48px; }
    h1 { margin-bottom: 4px; }
    .subtitle {
      color: #666;
      margin-bottom: 20px;
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .tailoring-status {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 24px;
      background: #e3f2fd;
      border-radius: 8px;
      margin: 20px 0;
    }
    .split-view {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 24px;
      margin-top: 20px;
    }
    .left-panel, .right-panel {
      min-height: 400px;
    }
    h3 { margin-top: 0; }
    .content-box {
      background: white;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      padding: 16px;
      white-space: pre-wrap;
      line-height: 1.6;
      font-size: 13px;
      max-height: 600px;
      overflow-y: auto;
    }
    .markdown {
      padding: 16px;
      margin-top: 8px;
    }
    .download-actions {
      margin-top: 16px;
      display: flex;
      gap: 12px;
    }
    .error-card {
      background: #ffebee;
      margin-top: 16px;
      mat-icon { color: #c62828; vertical-align: middle; margin-right: 8px; }
    }
    @media (max-width: 900px) {
      .split-view { grid-template-columns: 1fr; }
    }
  `,
})
export class ResumeTailorPage implements OnInit {
  jobId = input.required<number>();

  private careerService = inject(CareerService);

  loadingJob = signal(true);
  tailoring = signal(false);
  job = signal<JobListingDto | null>(null);
  tailored = signal<TailoredDocumentDto | null>(null);
  error = signal<string | null>(null);
  pdfUrl = signal('');

  ngOnInit() {
    this.careerService.getJobById(this.jobId()).subscribe({
      next: (j) => {
        this.job.set(j);
        this.loadingJob.set(false);
      },
      error: () => this.loadingJob.set(false),
    });
  }

  startTailoring() {
    this.tailoring.set(true);
    this.error.set(null);

    this.careerService.tailorResume(this.jobId()).subscribe({
      next: (doc) => {
        this.tailored.set(doc);
        this.pdfUrl.set(this.careerService.getPdfDownloadUrl(doc.id));
        this.tailoring.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Failed to tailor resume. Is your Claude API key configured?');
        this.tailoring.set(false);
      },
    });
  }
}
