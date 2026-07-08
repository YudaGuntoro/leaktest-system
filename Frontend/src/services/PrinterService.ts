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

export type Printer = {
  id: number;
  name: string;
  ipAddress: string;
  port: number;
  description: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type PrinterPayload = Pick<
  Printer,
  "name" | "ipAddress" | "port" | "description" | "isActive"
>;

export type PrinterQuery = {
  page?: number;
  limit?: number;
  search?: string;
  isActive?: boolean | null;
};

const normalizeQuery = (query: PrinterQuery) => ({
  page: query.page,
  limit: query.limit,
  search: query.search,
  isActive: query.isActive ?? undefined,
});

const PrinterService = {
  getPrinters: async (
    query: PrinterQuery = {},
    options?: ApiRequestOptions
  ) => {
    const response = await api.get<ApiListResponse<Printer>>("/api/printers", {
      ...options,
      params: normalizeQuery(query),
    });

    return response.data;
  },

  getPrinter: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.get<{
      success: boolean;
      message: string;
      data: Printer;
    }>(`/api/printers/${id}`, options);

    return response.data;
  },

  createPrinter: async (
    data: PrinterPayload,
    options?: ApiRequestOptions
  ) => {
    const response = await api.post<{
      success: boolean;
      message: string;
      data: Printer;
    }>("/api/printers", data, options);

    return response.data;
  },

  updatePrinter: async (
    id: number,
    data: Partial<PrinterPayload>,
    options?: ApiRequestOptions
  ) => {
    const response = await api.put<{
      success: boolean;
      message: string;
      data: Printer;
    }>(`/api/printers/${id}`, data, options);

    return response.data;
  },

  deletePrinter: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.delete<{ success: boolean; message: string }>(
      `/api/printers/${id}`,
      options
    );

    return response.data;
  },
};

export default PrinterService;
