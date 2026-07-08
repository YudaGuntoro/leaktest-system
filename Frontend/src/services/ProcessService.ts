import api, { ApiRequestOptions } from "@/utils/api";

export type ApiPagination = {
  page: number;
  limit: number;
  total: number;
  totalPage: number;
};

export type ApiListResponse<T> = {
  success: boolean;
  message: string;
  data: T[];
  pagination: ApiPagination;
};

export type ProcessParameter = {
  id: number;
  code: string;
  name: string;
  description: string;
  dataType: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
};

export type Process = {
  id: number;
  code: string;
  name: string;
  description: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  parameters: ProcessParameter[];
};

export type ProcessPayload = Pick<
  Process,
  "code" | "name" | "description" | "isActive"
> & {
  parameterIds: number[];
};

export type ProcessQuery = {
  page?: number;
  limit?: number;
  search?: string;
  isActive?: boolean | null;
};

const normalizeQuery = (query: ProcessQuery) => ({
  page: query.page,
  limit: query.limit,
  search: query.search,
  isActive: query.isActive ?? undefined,
});

const ProcessService = {
  getProcesses: async (
    query: ProcessQuery = {},
    options?: ApiRequestOptions
  ) => {
    const response = await api.get<ApiListResponse<Process>>(
      "/api/processes",
      {
        ...options,
        params: normalizeQuery(query),
      }
    );

    return response.data;
  },

  getProcess: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.get<{
      success: boolean;
      message: string;
      data: Process;
    }>(`/api/processes/${id}`, options);

    return response.data;
  },

  createProcess: async (
    data: ProcessPayload,
    options?: ApiRequestOptions
  ) => {
    const response = await api.post<{
      success: boolean;
      message: string;
      data: Process;
    }>("/api/processes", data, options);

    return response.data;
  },

  updateProcess: async (
    id: number,
    data: Partial<ProcessPayload>,
    options?: ApiRequestOptions
  ) => {
    const response = await api.put<{
      success: boolean;
      message: string;
      data: Process;
    }>(`/api/processes/${id}`, data, options);

    return response.data;
  },

  deleteProcess: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.delete<{ success: boolean; message: string }>(
      `/api/processes/${id}`,
      options
    );

    return response.data;
  },
};

export default ProcessService;
