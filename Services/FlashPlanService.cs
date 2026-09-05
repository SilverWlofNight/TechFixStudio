using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class FlashPlanService
{
    private static readonly HashSet<string>
        AllowedPartitions =
            new(
                new[]
                {
                    "boot",
                    "init_boot",
                    "vendor_boot",
                    "dtbo",
                    "vbmeta",
                    "vbmeta_system",
                    "vbmeta_vendor",
                    "recovery"
                },
                StringComparer.OrdinalIgnoreCase);

    public FlashPlan BuildPlan(
        FastbootInfo device,
        RomPackage package)
    {
        var plan =
            new FlashPlan
            {
                Serial = device.Serial,
                Product = device.Product,
                CurrentSlot = device.Slot,
                Unlocked = device.Unlocked,
                Secure = device.Secure,
                AntiRollback = device.AntiRollback,
                BootloaderUnlocked =
                    FastbootParser.IsTrue(
                        device.Unlocked)
            };

        if (!plan.BootloaderUnlocked)
        {
            plan.Warnings.Add(
                "Bootloader is not reported as unlocked.");
        }

        if (!string.IsNullOrWhiteSpace(
                package.Product) &&
            package.Product != "—" &&
            !string.IsNullOrWhiteSpace(
                device.Product))
        {
            plan.ProductMatch =
                device.Product.Contains(
                    package.Product,
                    StringComparison.OrdinalIgnoreCase)
                ||
                package.Product.Contains(
                    device.Product,
                    StringComparison.OrdinalIgnoreCase);

            if (!plan.ProductMatch)
            {
                plan.Warnings.Add(
                    $"Product mismatch: device={device.Product}, package={package.Product}");
            }
        }
        else
        {
            plan.ProductMatch = true;
        }

        foreach (var image in package.Images)
        {
            var allowed =
                AllowedPartitions.Contains(
                    image.Partition);

            var reason =
                allowed
                    ? "Allowed controlled partition."
                    : "Not allowed in generic flash queue.";

            if (image.Partition.Equals(
                    "super",
                    StringComparison.OrdinalIgnoreCase)
                ||
                image.Partition.Equals(
                    "system",
                    StringComparison.OrdinalIgnoreCase)
                ||
                image.Partition.Equals(
                    "vendor",
                    StringComparison.OrdinalIgnoreCase)
                ||
                image.Partition.Equals(
                    "userdata",
                    StringComparison.OrdinalIgnoreCase))
            {
                allowed = false;

                reason =
                    "Requires dedicated factory image workflow.";
            }

            plan.Items.Add(
                new FlashPlanItem
                {
                    Partition = image.Partition,
                    ImagePath = image.Path,
                    Sha256 = image.Sha256,
                    Size = image.Size,
                    Critical = image.Critical,
                    Allowed = allowed,
                    Reason = reason
                });
        }

        plan.SafeToProceed =
            plan.BootloaderUnlocked &&
            plan.ProductMatch &&
            plan.Warnings.Count == 0 &&
            plan.Items.Any(
                x => x.Allowed);

        return plan;
    }

    public bool IsAllowedPartition(
        string partition)
    {
        return AllowedPartitions.Contains(
            partition);
    }
}
