import api, { ApiRequestOptions } from "@/utils/api";
import { ApiListResponse } from "./ParameterService";

export type ProcessLogDetail = Record<string, unknown>;

export type ProcessLog = {
  id: number;
  issueNo: string;
  isActive: boolean;
  createdAt: string;
  details: ProcessLogDetail[];
};

export type ProcessLogQuery = {
  page?: number;
  limit?: number;
  issueNo?: string;
  partNumber?: string;
  isActive?: boolean | null;
};

const normalizeQuery = (query: ProcessLogQuery) => ({
  page: query.page,
  limit: query.limit,
  issueNo: query.issueNo,
  partNumber: query.partNumber,
  isActive: query.isActive ?? undefined,
});

const ProcessLogService = {
  getProcessLogs: async (
    query: ProcessLogQuery = {},
    options?: ApiRequestOptions
  ) => {
    const response = await api.get<ApiListResponse<ProcessLog>>(
      "/api/processlogs",
      {
        ...options,
        params: normalizeQuery(query),
      }
    );

    return response.data;
  },

  getProcessLog: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.get<{
      success: boolean;
      message: string;
      data: ProcessLog;
    }>(`/api/processlogs/${id}`, options);

    return response.data;
  },
};

export default ProcessLogService;
