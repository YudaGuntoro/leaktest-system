"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import UserService, {
  ApiListResponse,
  User,
  UserQuery,
} from "@/services/UserService";

type UseUsersOptions = UserQuery & {
  enabled?: boolean;
};

type UserQueryState = {
  page: number;
  limit: number;
  search: string;
  isActive?: boolean | null;
};

const getInitialQuery = (options: UseUsersOptions): UserQueryState => ({
  page: options.page ?? 1,
  limit: options.limit ?? 10,
  search: options.search ?? "",
  isActive: options.isActive,
});

export const useUsers = (options: UseUsersOptions = {}) => {
  const { enabled = true } = options;
  const [query, setQueryState] = useState<UserQueryState>(() =>
    getInitialQuery(options)
  );
  const [response, setResponse] = useState<ApiListResponse<User> | null>(null);
  const [isLoading, setIsLoading] = useState(enabled);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const requestQuery = useMemo(
    () => ({
      page: query.page,
      limit: query.limit,
      search: query.search,
      isActive: query.isActive,
    }),
    [query.isActive, query.limit, query.page, query.search]
  );

  const startRequest = useCallback(() => {
    if (enabled) {
      setIsLoading(true);
      setError(null);
    }
  }, [enabled]);

  const setPage = useCallback(
    (page: number) => {
      startRequest();
      setQueryState((current) => ({
        ...current,
        page,
      }));
    },
    [startRequest]
  );

  const setLimit = useCallback(
    (limit: number) => {
      startRequest();
      setQueryState((current) => ({
        ...current,
        limit,
        page: 1,
      }));
    },
    [startRequest]
  );

  const setSearch = useCallback(
    (search: string) => {
      startRequest();
      setQueryState((current) => ({
        ...current,
        page: 1,
        search,
      }));
    },
    [startRequest]
  );

  const setIsActive = useCallback(
    (isActive?: boolean | null) => {
      startRequest();
      setQueryState((current) => ({
        ...current,
        isActive,
        page: 1,
      }));
    },
    [startRequest]
  );

  const setQuery = useCallback(
    (nextQuery: Partial<UserQueryState>) => {
      startRequest();
      setQueryState((current) => ({
        ...current,
        ...nextQuery,
      }));
    },
    [startRequest]
  );

  const refetch = useCallback(() => {
    startRequest();
    setReloadKey((current) => current + 1);
  }, [startRequest]);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const controller = new AbortController();

    UserService.getUsers(requestQuery, {
      signal: controller.signal,
    })
      .then((result) => {
        setResponse(result);
      })
      .catch((fetchError: unknown) => {
        if (
          fetchError instanceof DOMException &&
          fetchError.name === "AbortError"
        ) {
          return;
        }

        setError(
          fetchError instanceof Error ? fetchError.message : "Failed to fetch users"
        );
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [enabled, reloadKey, requestQuery]);

  return {
    data: response?.data ?? [],
    error,
    isLoading,
    pagination: response?.pagination,
    query,
    refetch,
    response,
    setIsActive,
    setLimit,
    setPage,
    setQuery,
    setSearch,
  };
};
