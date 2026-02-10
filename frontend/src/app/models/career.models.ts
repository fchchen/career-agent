export interface JobListingDto {
  id: number;
  externalId: string;
  source: string;
  title: string;
  company: string;
  location: string;
  description: string;
  url: string;
  salary: string | null;
  relevanceScore: number;
  matchedSkills: string[];
  missingSkills: string[];
  status: JobStatus;
  postedAt: string;
  fetchedAt: string;
}

export type JobStatus = 'New' | 'Viewed' | 'Applied' | 'Dismissed';

export interface JobSearchRequest {
  query?: string;
  location?: string;
  remoteOnly?: boolean;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface DashboardResponse {
  stats: DashboardStats;
  topJobs: JobListingDto[];
  recentJobs: JobListingDto[];
}

export interface DashboardStats {
  totalJobs: number;
  newJobs: number;
  appliedJobs: number;
  dismissedJobs: number;
  averageScore: number;
}

export interface MasterResumeDto {
  id: number;
  name: string;
  content: string;
  rawMarkdown: string;
  updatedAt: string;
}

export interface MasterResumeUpdateRequest {
  content: string;
  rawMarkdown: string;
}

export interface TailoredDocumentDto {
  id: number;
  jobListingId: number;
  jobTitle: string;
  company: string;
  tailoredResumeMarkdown: string;
  coverLetterMarkdown: string;
  pdfPath: string | null;
  createdAt: string;
}

export interface TailorRequest {
  masterResumeId?: number;
}

export interface JobStatusUpdateRequest {
  status: JobStatus;
}
