import { apiClient } from "@/lib/api-client"
import type { SetupRequest, SetupResult, SetupStatus } from "@/lib/api-types"

export async function getSetupStatus() {
  const response = await apiClient.get<SetupStatus>("/setup/status")
  return response.data
}

export async function initializeSetup(request: SetupRequest) {
  const response = await apiClient.post<SetupResult>("/setup", request)
  return response.data
}
