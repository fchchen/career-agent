import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, map, shareReplay } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  DashboardResponse,
  GeocodeResponse,
  JobListingDto,
  JobSearchRequest,
  JobStatus,
  JobStatusUpdateRequest,
  LocationFilter,
  MasterResumeDto,
  MasterResumeUpdateRequest,
  PagedResponse,
  SearchProfileDto,
  SearchProfileUpdateRequest,
  TailoredDocumentDto,
  TailorRequest,
} from '../models/career.models';

@Injectable({ providedIn: 'root' })
export class CareerService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  private staticData = environment.staticData;

  isLoading = signal(false);
  error = signal<string | null>(null);

  private cache = new Map<string, Observable<any>>();

  private getCached<T>(url: string): Observable<T> {
    if (!this.cache.has(url)) {
      this.cache.set(url, this.http.get<T>(url).pipe(shareReplay(1)));
    }
    return this.cache.get(url)!;
  }

  // Dashboard
  getDashboard(): Observable<DashboardResponse> {
    if (this.staticData) {
      return this.getCached<DashboardResponse>(`${this.apiUrl}/dashboard.json`);
    }
    return this.http.get<DashboardResponse>(`${this.apiUrl}/dashboard`);
  }

  // Jobs
  getJobs(
    page = 1,
    pageSize = 20,
    status?: JobStatus,
    sortBy?: string,
    postedWithinHours?: number,
    locationFilter?: LocationFilter
  ): Observable<PagedResponse<JobListingDto>> {
    if (this.staticData) {
      return this.getCached<PagedResponse<JobListingDto>>(`${this.apiUrl}/jobs.json`).pipe(
        map((data) => {
          let items = [...data.items];
          if (status) items = items.filter((j) => j.status === status);
          if (postedWithinHours) {
            const cutoff = new Date(Date.now() - postedWithinHours * 60 * 60 * 1000);
            items = items.filter((j) => new Date(j.postedAt) >= cutoff);
          }
          if (sortBy === 'date') {
            items.sort(
              (a, b) => new Date(b.postedAt).getTime() - new Date(a.postedAt).getTime()
            );
          } else {
            items.sort((a, b) => b.relevanceScore - a.relevanceScore);
          }
          const start = (page - 1) * pageSize;
          return {
            items: items.slice(start, start + pageSize),
            totalCount: items.length,
            page,
            pageSize,
          };
        })
      );
    }
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (sortBy) params = params.set('sortBy', sortBy);
    if (postedWithinHours) params = params.set('postedWithinHours', postedWithinHours);
    if (locationFilter) {
      params = params
        .set('homeLatitude', locationFilter.homeLatitude)
        .set('homeLongitude', locationFilter.homeLongitude)
        .set('radiusMiles', locationFilter.radiusMiles)
        .set('includeRemote', locationFilter.includeRemote);
    }
    return this.http.get<PagedResponse<JobListingDto>>(`${this.apiUrl}/jobs`, { params });
  }

  geocodeAddress(address: string): Observable<GeocodeResponse> {
    return this.http.post<GeocodeResponse>(`${this.apiUrl}/jobs/geocode`, { address });
  }

  getJobById(id: number): Observable<JobListingDto> {
    if (this.staticData) {
      return this.getCached<PagedResponse<JobListingDto>>(`${this.apiUrl}/jobs.json`).pipe(
        map((data) => data.items.find((j) => j.id === id)!),
      );
    }
    return this.http.get<JobListingDto>(`${this.apiUrl}/jobs/${id}`);
  }

  searchJobs(request: JobSearchRequest): Observable<JobListingDto[]> {
    if (this.staticData) {
      return of([]);
    }
    return this.http.post<JobListingDto[]>(`${this.apiUrl}/jobs/search`, request);
  }

  updateJobStatus(id: number, status: JobStatus): Observable<void> {
    if (this.staticData) return of(undefined);
    const body: JobStatusUpdateRequest = { status };
    return this.http.patch<void>(`${this.apiUrl}/jobs/${id}/status`, body);
  }

  // Resume
  getMasterResume(): Observable<MasterResumeDto> {
    if (this.staticData) {
      return this.getCached<MasterResumeDto>(`${this.apiUrl}/resume.json`);
    }
    return this.http.get<MasterResumeDto>(`${this.apiUrl}/resume`);
  }

  updateMasterResume(request: MasterResumeUpdateRequest): Observable<MasterResumeDto> {
    return this.http.put<MasterResumeDto>(`${this.apiUrl}/resume`, request);
  }

  tailorResume(jobId: number, request?: TailorRequest): Observable<TailoredDocumentDto> {
    return this.http.post<TailoredDocumentDto>(
      `${this.apiUrl}/resume/tailor/${jobId}`,
      request ?? {}
    );
  }

  getTailoredDocument(id: number): Observable<TailoredDocumentDto> {
    return this.http.get<TailoredDocumentDto>(`${this.apiUrl}/resume/tailored/${id}`);
  }

  getTailoredDocumentsForJob(jobId: number): Observable<TailoredDocumentDto[]> {
    return this.http.get<TailoredDocumentDto[]>(`${this.apiUrl}/resume/tailored/job/${jobId}`);
  }

  getPdfDownloadUrl(tailoredId: number): string {
    return `${this.apiUrl}/resume/tailored/${tailoredId}/pdf`;
  }

  // Profile
  getProfile(): Observable<SearchProfileDto> {
    if (this.staticData) {
      return of({
        id: 1,
        name: 'Default',
        query: 'Senior Software Engineer .NET Angular',
        location: 'Rochester Hills, MI 48307',
        radiusMiles: 50,
        remoteOnly: false,
        requiredSkills: ['.NET', 'C#', 'Angular', 'TypeScript', 'SQL Server', 'Azure', 'AWS'],
        preferredSkills: ['REST API', 'Entity Framework', 'Git', 'CI/CD', 'Agile', 'JavaScript', 'HTML/CSS', 'Docker', 'Azure DevOps'],
        titleKeywords: ['Senior Software Engineer', 'Senior Software Developer', 'Senior Full Stack Developer', 'Senior .NET Developer', 'Senior Backend Engineer', 'Staff Software Engineer', 'Lead Software Engineer', 'Principal Software Engineer'],
        negativeTitleKeywords: ['Junior', 'Intern', 'Entry Level', 'Associate', 'Data Scientist', 'Machine Learning', 'DevOps', 'SRE', 'QA', 'Test Engineer', 'Security Engineer'],
        createdAt: new Date().toISOString(),
      });
    }
    return this.http.get<SearchProfileDto>(`${this.apiUrl}/profile`);
  }

  updateProfile(request: SearchProfileUpdateRequest): Observable<SearchProfileDto> {
    return this.http.put<SearchProfileDto>(`${this.apiUrl}/profile`, request);
  }
}
