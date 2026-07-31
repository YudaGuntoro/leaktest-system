"use client";

import React, { useEffect, useState } from "react";
import { Modal } from "@/components/ui/modal";
import { useToast } from "@/context/ToastContext";
import UserService, {
  User,
  UserCreatePayload,
  UserRole,
  UserUpdatePayload,
} from "@/services/UserService";

interface UserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  user: User | null;
}

type UserFormData = {
  full_name: string;
  username: string;
  email: string;
  phone: string;
  password: string;
  roles_id: string;
  is_active: boolean;
};

const initialFormData: UserFormData = {
  full_name: "",
  username: "",
  email: "",
  phone: "",
  password: "",
  roles_id: "4",
  is_active: true,
};

const fallbackRoleOptions: UserRole[] = [
  { id: 1, role_name: "ADMIN", description: "Administrator", is_active: true, created_at: "", updated_at: "" },
  { id: 2, role_name: "SUPERVISOR", description: "Supervisor", is_active: true, created_at: "", updated_at: "" },
  { id: 3, role_name: "OPERATOR", description: "Operator", is_active: true, created_at: "", updated_at: "" },
  { id: 4, role_name: "VIEWER", description: "Viewer", is_active: true, created_at: "", updated_at: "" },
];

const getErrorMessage = (error: unknown, fallback: string) =>
  error instanceof Error ? error.message : fallback;

export default function UserModal({
  isOpen,
  onClose,
  onSuccess,
  user,
}: UserModalProps) {
  const toast = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<UserFormData>(initialFormData);
  const [roleOptions, setRoleOptions] = useState<UserRole[]>(fallbackRoleOptions);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    UserService.getRoles()
      .then((response) => {
        if (response.data.length > 0) {
          setRoleOptions(response.data);
        }
      })
      .catch(() => {
        setRoleOptions(fallbackRoleOptions);
      });
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (user) {
      setFormData({
        full_name: user.full_name || "",
        username: user.username || "",
        email: user.email || "",
        phone: user.phone || "",
        password: "",
        roles_id: String(user.roles_id || 4),
        is_active: user.is_active ?? true,
      });
      return;
    }

    setFormData(initialFormData);
  }, [isOpen, user]);

  const handleChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, type, value } = event.target;
    const checked = (event.target as HTMLInputElement).checked;

    setFormData((current) => ({
      ...current,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!user && !formData.password.trim()) {
      toast.error({
        title: "Error",
        message: "Password is required",
      });
      return;
    }

    setIsSubmitting(true);

    const updatePayload: UserUpdatePayload = {
      username: formData.username.trim(),
      full_name: formData.full_name.trim(),
      email: formData.email.trim() || null,
      phone: formData.phone.trim() || null,
      roles_id: Number(formData.roles_id),
      is_active: formData.is_active,
    };

    if (user && formData.password.trim()) {
      updatePayload.password = formData.password.trim();
    }

    const createPayload: UserCreatePayload = {
      ...updatePayload,
      password: formData.password.trim(),
    };

    try {
      if (user) {
        await UserService.updateUser(user.id, updatePayload);
        toast.success({
          title: "Success",
          message: "User updated successfully",
        });
      } else {
        await UserService.createUser(createPayload);
        toast.success({
          title: "Success",
          message: "User created successfully",
        });
      }

      onSuccess();
      onClose();
    } catch (error: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(
          error,
          `Failed to ${user ? "update" : "create"} user`
        ),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} className="max-w-[560px] p-6">
      <h3 className="mb-4 text-xl font-semibold text-gray-800 dark:text-white/90">
        {user ? "Update User" : "Register User"}
      </h3>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Full Name
            </label>
            <input
              type="text"
              name="full_name"
              value={formData.full_name}
              onChange={handleChange}
              required
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
              placeholder="Enter full name"
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Username
            </label>
            <input
              type="text"
              name="username"
              value={formData.username}
              onChange={handleChange}
              required
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
              placeholder="Enter username"
            />
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Email (Optional)
            </label>
            <input
              type="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
              placeholder="Enter email"
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Phone (Optional)
            </label>
            <input
              type="text"
              name="phone"
              value={formData.phone}
              onChange={handleChange}
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
              placeholder="Enter phone"
            />
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Password
            </label>
            <input
              type="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required={!user}
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
              placeholder={user ? "Leave blank to keep" : "Enter password"}
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Role
            </label>
            <select
              name="roles_id"
              value={formData.roles_id}
              onChange={handleChange}
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            >
              {roleOptions.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.role_name}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="user-is-active"
            name="is_active"
            checked={formData.is_active}
            onChange={handleChange}
            className="h-4 w-4 rounded border-gray-300 text-brand-500 focus:ring-brand-500"
          />
          <label
            htmlFor="user-is-active"
            className="text-sm font-medium text-gray-700 dark:text-gray-300"
          >
            Active
          </label>
        </div>

        <div className="mt-4 flex justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-500/50 disabled:opacity-50"
          >
            {isSubmitting ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </Modal>
  );
}
