import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CareerService } from '../../services/career.service';
import { SearchProfileDto, SearchProfileUpdateRequest } from '../../models/career.models';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatChipsModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
  ],
  template: `
    @if (loading()) {
      <div class="center"><mat-spinner diameter="40" /></div>
    } @else {
      <div class="settings">
        <h1>Settings</h1>

        <mat-card>
          <mat-card-header><mat-card-title>Search</mat-card-title></mat-card-header>
          <mat-card-content>
            <div class="form-row">
              <mat-form-field appearance="outline" class="flex-field">
                <mat-label>Search Query</mat-label>
                <input matInput [(ngModel)]="query" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="flex-field">
                <mat-label>Location</mat-label>
                <input matInput [(ngModel)]="location" />
              </mat-form-field>
            </div>
            <div class="form-row">
              <mat-form-field appearance="outline" class="radius-field">
                <mat-label>Radius (miles)</mat-label>
                <input matInput type="number" [(ngModel)]="radiusMiles" />
              </mat-form-field>
              <mat-checkbox [(ngModel)]="remoteOnly">Remote only</mat-checkbox>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header><mat-card-title>Skills</mat-card-title></mat-card-header>
          <mat-card-content>
            <div class="chip-section">
              <label>Required Skills (core, weight 1.0)</label>
              <mat-chip-set>
                @for (skill of requiredSkills(); track skill) {
                  <mat-chip (removed)="removeChip('requiredSkills', skill)">
                    {{ skill }}
                    <button matChipRemove><mat-icon>cancel</mat-icon></button>
                  </mat-chip>
                }
              </mat-chip-set>
              <div class="add-row">
                <mat-form-field appearance="outline" class="add-field">
                  <mat-label>Add skill</mat-label>
                  <input matInput [(ngModel)]="newRequiredSkill" (keydown.enter)="addChip('requiredSkills', newRequiredSkill); newRequiredSkill = ''" />
                </mat-form-field>
                <button mat-icon-button (click)="addChip('requiredSkills', newRequiredSkill); newRequiredSkill = ''">
                  <mat-icon>add</mat-icon>
                </button>
              </div>
            </div>

            <div class="chip-section">
              <label>Preferred Skills (strong, weight 0.6)</label>
              <mat-chip-set>
                @for (skill of preferredSkills(); track skill) {
                  <mat-chip (removed)="removeChip('preferredSkills', skill)">
                    {{ skill }}
                    <button matChipRemove><mat-icon>cancel</mat-icon></button>
                  </mat-chip>
                }
              </mat-chip-set>
              <div class="add-row">
                <mat-form-field appearance="outline" class="add-field">
                  <mat-label>Add skill</mat-label>
                  <input matInput [(ngModel)]="newPreferredSkill" (keydown.enter)="addChip('preferredSkills', newPreferredSkill); newPreferredSkill = ''" />
                </mat-form-field>
                <button mat-icon-button (click)="addChip('preferredSkills', newPreferredSkill); newPreferredSkill = ''">
                  <mat-icon>add</mat-icon>
                </button>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header><mat-card-title>Title Keywords</mat-card-title></mat-card-header>
          <mat-card-content>
            <div class="chip-section">
              <label>Title Keywords (exact match = 1.0 title score)</label>
              <mat-chip-set>
                @for (kw of titleKeywords(); track kw) {
                  <mat-chip (removed)="removeChip('titleKeywords', kw)">
                    {{ kw }}
                    <button matChipRemove><mat-icon>cancel</mat-icon></button>
                  </mat-chip>
                }
              </mat-chip-set>
              <div class="add-row">
                <mat-form-field appearance="outline" class="add-field">
                  <mat-label>Add keyword</mat-label>
                  <input matInput [(ngModel)]="newTitleKeyword" (keydown.enter)="addChip('titleKeywords', newTitleKeyword); newTitleKeyword = ''" />
                </mat-form-field>
                <button mat-icon-button (click)="addChip('titleKeywords', newTitleKeyword); newTitleKeyword = ''">
                  <mat-icon>add</mat-icon>
                </button>
              </div>
            </div>

            <div class="chip-section">
              <label>Negative Title Keywords (penalized titles)</label>
              <mat-chip-set>
                @for (kw of negativeTitleKeywords(); track kw) {
                  <mat-chip (removed)="removeChip('negativeTitleKeywords', kw)">
                    {{ kw }}
                    <button matChipRemove><mat-icon>cancel</mat-icon></button>
                  </mat-chip>
                }
              </mat-chip-set>
              <div class="add-row">
                <mat-form-field appearance="outline" class="add-field">
                  <mat-label>Add keyword</mat-label>
                  <input matInput [(ngModel)]="newNegativeTitleKeyword" (keydown.enter)="addChip('negativeTitleKeywords', newNegativeTitleKeyword); newNegativeTitleKeyword = ''" />
                </mat-form-field>
                <button mat-icon-button (click)="addChip('negativeTitleKeywords', newNegativeTitleKeyword); newNegativeTitleKeyword = ''">
                  <mat-icon>add</mat-icon>
                </button>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <div class="actions">
          <button mat-raised-button color="primary" (click)="save()" [disabled]="saving()">
            @if (saving()) {
              <mat-spinner diameter="20" />
            } @else {
              Save
            }
          </button>
        </div>
      </div>
    }
  `,
  styles: `
    .settings {
      padding: 24px;
      max-width: 800px;
      margin: 0 auto;
    }
    h1 { margin-bottom: 16px; }
    mat-card { margin-bottom: 16px; }
    mat-card-content { padding-top: 16px; }
    .form-row {
      display: flex;
      gap: 12px;
      align-items: center;
      flex-wrap: wrap;
    }
    .flex-field { flex: 1; min-width: 200px; }
    .radius-field { width: 150px; }
    .chip-section { margin-bottom: 16px; }
    .chip-section label {
      display: block;
      font-size: 13px;
      color: var(--mat-sys-on-surface-variant);
      margin-bottom: 8px;
    }
    .add-row {
      display: flex;
      gap: 8px;
      align-items: center;
      margin-top: 8px;
    }
    .add-field { width: 250px; }
    .actions { display: flex; justify-content: flex-end; margin-top: 8px; }
    .center { display: flex; justify-content: center; padding: 48px; }
  `,
})
export class SettingsPage implements OnInit {
  private careerService = inject(CareerService);
  private snackBar = inject(MatSnackBar);

  loading = signal(true);
  saving = signal(false);

  query = '';
  location = '';
  radiusMiles = 50;
  remoteOnly = false;

  requiredSkills = signal<string[]>([]);
  preferredSkills = signal<string[]>([]);
  titleKeywords = signal<string[]>([]);
  negativeTitleKeywords = signal<string[]>([]);

  newRequiredSkill = '';
  newPreferredSkill = '';
  newTitleKeyword = '';
  newNegativeTitleKeyword = '';

  ngOnInit() {
    this.careerService.getProfile().subscribe({
      next: (profile) => {
        this.query = profile.query;
        this.location = profile.location;
        this.radiusMiles = profile.radiusMiles;
        this.remoteOnly = profile.remoteOnly;
        this.requiredSkills.set([...profile.requiredSkills]);
        this.preferredSkills.set([...profile.preferredSkills]);
        this.titleKeywords.set([...profile.titleKeywords]);
        this.negativeTitleKeywords.set([...profile.negativeTitleKeywords]);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load profile', 'Close', { duration: 3000 });
      },
    });
  }

  addChip(field: 'requiredSkills' | 'preferredSkills' | 'titleKeywords' | 'negativeTitleKeywords', value: string) {
    const trimmed = value.trim();
    if (!trimmed) return;
    const current = this[field]();
    if (!current.includes(trimmed)) {
      this[field].set([...current, trimmed]);
    }
  }

  removeChip(field: 'requiredSkills' | 'preferredSkills' | 'titleKeywords' | 'negativeTitleKeywords', value: string) {
    this[field].set(this[field]().filter((v) => v !== value));
  }

  save() {
    this.saving.set(true);
    const request: SearchProfileUpdateRequest = {
      query: this.query,
      location: this.location,
      radiusMiles: this.radiusMiles,
      remoteOnly: this.remoteOnly,
      requiredSkills: this.requiredSkills(),
      preferredSkills: this.preferredSkills(),
      titleKeywords: this.titleKeywords(),
      negativeTitleKeywords: this.negativeTitleKeywords(),
    };
    this.careerService.updateProfile(request).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Profile saved', 'Close', { duration: 3000 });
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to save profile', 'Close', { duration: 3000 });
      },
    });
  }
}
