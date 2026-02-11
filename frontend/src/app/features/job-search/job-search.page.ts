import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CareerService } from '../../services/career.service';
import { JobListingDto, JobStatus, LocationFilter } from '../../models/career.models';
import { JobCardComponent } from '../../shared/components/job-card/job-card.component';

@Component({
  selector: 'app-job-search',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
    MatTooltipModule,
    JobCardComponent,
  ],
  template: `
    <div class="job-search">
      <h1>Job Search</h1>

      <mat-card class="search-bar">
        <mat-card-content>
          <div class="search-row">
            <mat-form-field appearance="outline" class="flex-field">
              <mat-label>Search query</mat-label>
              <input matInput [(ngModel)]="query" placeholder="Senior Software Engineer .NET Angular" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="flex-field">
              <mat-label>Location</mat-label>
              <input matInput [(ngModel)]="location" placeholder="United States" />
            </mat-form-field>
            <mat-checkbox [(ngModel)]="remoteOnly">Remote only</mat-checkbox>
            <button mat-raised-button color="primary" (click)="search()" [disabled]="searching()">
              <mat-icon>search</mat-icon> Search
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <div class="filters-row">
        <mat-form-field appearance="outline">
          <mat-label>Status</mat-label>
          <mat-select [(ngModel)]="statusFilter" (selectionChange)="loadJobs()">
            <mat-option [value]="undefined">All</mat-option>
            <mat-option value="New">New</mat-option>
            <mat-option value="Viewed">Viewed</mat-option>
            <mat-option value="Applied">Applied</mat-option>
            <mat-option value="Dismissed">Dismissed</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Posted within</mat-label>
          <mat-select [(ngModel)]="postedWithinHours" (selectionChange)="loadJobs()">
            <mat-option [value]="undefined">Any time</mat-option>
            <mat-option [value]="24">Last 24 hours</mat-option>
            <mat-option [value]="72">Last 3 days</mat-option>
            <mat-option [value]="168">Last week</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Sort by</mat-label>
          <mat-select [(ngModel)]="sortBy" (selectionChange)="loadJobs()">
            <mat-option value="score">Score</mat-option>
            <mat-option value="date">Date</mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      <mat-card class="location-filter">
        <mat-card-content>
          <div class="location-row">
            <mat-form-field appearance="outline" class="flex-field">
              <mat-label>Home address</mat-label>
              <input matInput [(ngModel)]="homeAddress" placeholder="Rochester Hills, MI 48307" />
            </mat-form-field>
            <button mat-stroked-button (click)="geocodeHome()" [disabled]="geocoding()">
              <mat-icon>my_location</mat-icon> Set
            </button>
            <mat-form-field appearance="outline" class="radius-field">
              <mat-label>Radius (mi)</mat-label>
              <input matInput type="number" [(ngModel)]="radiusMiles" (change)="onLocationFilterChange()" />
            </mat-form-field>
            <mat-checkbox [(ngModel)]="includeRemote" (change)="onLocationFilterChange()">Include remote</mat-checkbox>
            @if (homeCoords()) {
              <button mat-icon-button (click)="clearLocationFilter()" matTooltip="Clear location filter">
                <mat-icon>close</mat-icon>
              </button>
            }
          </div>
          @if (homeCoords()) {
            <div class="location-status">Filtering around: {{ homeCoords()!.displayName }}</div>
          }
        </mat-card-content>
      </mat-card>

      @if (searching()) {
        <div class="center"><mat-spinner diameter="40" /></div>
      }

      @if (loading()) {
        <div class="center"><mat-spinner diameter="40" /></div>
      } @else {
        <div class="job-list">
          @for (job of jobs(); track job.id) {
            <app-job-card [job]="job" />
          }
          @empty {
            <p class="empty">No jobs found. Try running a search above.</p>
          }
        </div>

        @if (totalCount() > 0) {
          <mat-paginator
            [length]="totalCount()"
            [pageSize]="pageSize"
            [pageIndex]="page() - 1"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="onPage($event)"
          />
        }
      }
    </div>
  `,
  styles: `
    .job-search {
      padding: 24px;
      max-width: 1000px;
      margin: 0 auto;
    }
    h1 { margin-bottom: 16px; }
    .search-bar { margin-bottom: 16px; }
    .search-row {
      display: flex;
      gap: 12px;
      align-items: center;
      flex-wrap: wrap;
    }
    .flex-field { flex: 1; min-width: 200px; }
    .filters-row {
      display: flex;
      gap: 12px;
      margin-bottom: 16px;
    }
    .center { display: flex; justify-content: center; padding: 48px; }
    .job-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .location-filter { margin-bottom: 16px; }
    .location-row {
      display: flex;
      gap: 12px;
      align-items: center;
      flex-wrap: wrap;
    }
    .radius-field { width: 120px; }
    .location-status {
      font-size: 12px;
      color: var(--mat-sys-on-surface-variant);
      margin-top: -8px;
    }
    .empty { color: var(--mat-sys-on-surface-variant); text-align: center; padding: 24px; }
  `,
})
export class JobSearchPage implements OnInit {
  private careerService = inject(CareerService);

  query = 'Senior Software Engineer .NET Angular';
  location = 'United States';
  remoteOnly = false;
  statusFilter?: JobStatus;
  postedWithinHours?: number = 72;
  sortBy = 'score';
  pageSize = 20;

  homeAddress = '';
  radiusMiles = 30;
  includeRemote = true;
  homeCoords = signal<{ latitude: number; longitude: number; displayName: string } | null>(null);
  geocoding = signal(false);

  loading = signal(true);
  searching = signal(false);
  jobs = signal<JobListingDto[]>([]);
  totalCount = signal(0);
  page = signal(1);

  ngOnInit() {
    this.loadJobs();
  }

  loadJobs() {
    this.loading.set(true);
    const coords = this.homeCoords();
    const locationFilter: LocationFilter | undefined = coords
      ? { homeLatitude: coords.latitude, homeLongitude: coords.longitude, radiusMiles: this.radiusMiles, includeRemote: this.includeRemote }
      : undefined;
    this.careerService
      .getJobs(this.page(), this.pageSize, this.statusFilter, this.sortBy, this.postedWithinHours, locationFilter)
      .subscribe({
        next: (res) => {
          this.jobs.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  search() {
    this.searching.set(true);
    this.careerService
      .searchJobs({ query: this.query, location: this.location, remoteOnly: this.remoteOnly })
      .subscribe({
        next: (results) => {
          this.searching.set(false);
          this.loadJobs(); // Refresh from DB after search populates it
        },
        error: () => this.searching.set(false),
      });
  }

  geocodeHome() {
    if (!this.homeAddress.trim()) return;
    this.geocoding.set(true);
    this.careerService.geocodeAddress(this.homeAddress).subscribe({
      next: (res) => {
        this.homeCoords.set(res);
        this.geocoding.set(false);
        this.page.set(1);
        this.loadJobs();
      },
      error: () => this.geocoding.set(false),
    });
  }

  clearLocationFilter() {
    this.homeCoords.set(null);
    this.page.set(1);
    this.loadJobs();
  }

  onLocationFilterChange() {
    if (this.homeCoords()) {
      this.page.set(1);
      this.loadJobs();
    }
  }

  onPage(event: PageEvent) {
    this.page.set(event.pageIndex + 1);
    this.pageSize = event.pageSize;
    this.loadJobs();
  }
}
