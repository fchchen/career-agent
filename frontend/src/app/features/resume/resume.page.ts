import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CareerService } from '../../services/career.service';

@Component({
  selector: 'app-resume',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
  ],
  template: `
    <div class="resume-page">
      <h1>Master Resume</h1>
      <p class="subtitle">Edit your master resume below. This is used as the base when tailoring for specific jobs.</p>

      @if (loading()) {
        <div class="center"><mat-spinner diameter="40" /></div>
      } @else {
        <mat-form-field appearance="outline" class="resume-field">
          <mat-label>Resume (Markdown)</mat-label>
          <textarea
            matInput
            [(ngModel)]="rawMarkdown"
            rows="30"
            placeholder="Paste your resume in Markdown format..."
          ></textarea>
        </mat-form-field>

        <div class="actions">
          <button
            mat-raised-button
            color="primary"
            (click)="save()"
            [disabled]="saving()"
          >
            @if (saving()) {
              <mat-spinner diameter="20" />
            }
            @if (!saving()) {
              <mat-icon>save</mat-icon>
            }
            {{ saving() ? '' : 'Save Resume' }}
          </button>
          @if (updatedAt()) {
            <span class="last-saved">Last saved: {{ updatedAt() | date: 'medium' }}</span>
          }
        </div>
      }

      @if (error()) {
        <mat-card class="error-card">
          <mat-card-content>
            <mat-icon>error</mat-icon> {{ error() }}
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: `
    .resume-page {
      padding: 24px;
      max-width: 900px;
      margin: 0 auto;
    }
    .center { display: flex; justify-content: center; padding: 48px; }
    h1 { margin-bottom: 4px; }
    .subtitle {
      color: var(--mat-sys-on-surface-variant);
      margin-bottom: 20px;
    }
    .resume-field {
      width: 100%;
    }
    textarea {
      font-family: 'Fira Code', 'Cascadia Code', monospace;
      font-size: 13px;
      line-height: 1.6;
    }
    .actions {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-top: 8px;
    }
    .last-saved {
      color: var(--mat-sys-on-surface-variant);
      font-size: 12px;
    }
    .error-card {
      background: #b71c1c;
      margin-top: 16px;
      mat-icon { color: #ef9a9a; vertical-align: middle; margin-right: 8px; }
    }
  `,
})
export class ResumePage implements OnInit {
  private careerService = inject(CareerService);
  private snackBar = inject(MatSnackBar);

  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);
  updatedAt = signal<string | null>(null);
  rawMarkdown = '';

  ngOnInit() {
    this.careerService.getMasterResume().subscribe({
      next: (resume) => {
        this.rawMarkdown = resume.rawMarkdown || resume.content || '';
        this.updatedAt.set(resume.updatedAt);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  save() {
    this.saving.set(true);
    this.error.set(null);

    this.careerService
      .updateMasterResume({ content: this.rawMarkdown, rawMarkdown: this.rawMarkdown })
      .subscribe({
        next: (resume) => {
          this.updatedAt.set(resume.updatedAt);
          this.saving.set(false);
          this.snackBar.open('Resume saved!', 'OK', { duration: 3000 });
        },
        error: (err) => {
          this.error.set(err.error?.message ?? 'Failed to save resume');
          this.saving.set(false);
        },
      });
  }
}
