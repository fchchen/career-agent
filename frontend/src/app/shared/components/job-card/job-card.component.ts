import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { JobListingDto } from '../../../models/career.models';
import { ScoreBadgeComponent } from '../score-badge/score-badge.component';

@Component({
  selector: 'app-job-card',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatChipsModule, MatIconModule, ScoreBadgeComponent],
  template: `
    <mat-card class="job-card" [routerLink]="['/jobs', job().id]">
      <mat-card-header>
        <mat-card-title class="title-row">
          <span class="job-title">{{ job().title }}</span>
          <app-score-badge [score]="job().relevanceScore" />
        </mat-card-title>
        <mat-card-subtitle>
          {{ job().company }} &middot; {{ job().location }}
          @if (job().salary) {
            &middot; {{ job().salary }}
          }
          @if (job().isRemote) {
            <span class="remote-badge">Remote</span>
          }
        </mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <div class="skills">
          @for (skill of job().matchedSkills.slice(0, 6); track skill) {
            <mat-chip class="matched">{{ skill }}</mat-chip>
          }
          @for (skill of job().missingSkills.slice(0, 3); track skill) {
            <mat-chip class="missing">{{ skill }}</mat-chip>
          }
        </div>
        <div class="meta">
          <span class="source">
            <mat-icon>language</mat-icon> {{ job().source }}
          </span>
          <span class="date">{{ job().postedAt | date: 'mediumDate' }}</span>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    .job-card {
      cursor: pointer;
      transition: box-shadow 0.2s;
      &:hover {
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      }
    }
    .title-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 12px;
    }
    .job-title {
      flex: 1;
    }
    .skills {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
      margin: 12px 0;
    }
    .matched {
      --mdc-chip-elevated-container-color: #1b5e20;
      --mdc-chip-label-text-color: #a5d6a7;
    }
    .missing {
      --mdc-chip-elevated-container-color: #b71c1c;
      --mdc-chip-label-text-color: #ef9a9a;
    }
    .remote-badge {
      background: #1565c0;
      color: #bbdefb;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 500;
      margin-left: 4px;
    }
    .meta {
      display: flex;
      justify-content: space-between;
      align-items: center;
      color: var(--mat-sys-on-surface-variant);
      font-size: 12px;
      margin-top: 8px;
    }
    .source {
      display: flex;
      align-items: center;
      gap: 4px;
      mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
      }
    }
  `,
})
export class JobCardComponent {
  job = input.required<JobListingDto>();
}
