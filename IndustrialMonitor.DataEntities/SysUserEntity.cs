
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities
{
    /// <summary>
    /// ϵͳ�û���
    /// </summary>
    [Table("monitor_SysUsers")]
    public class SysUserEntity
    {
        /// <summary>
        /// �û�ID
        /// </summary>
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// ��¼�˺�
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// ����(������)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// �Ƿ����Ա
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// ��ʵ����
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// �Ա�
        /// </summary>
        public bool Gender { get; set; } = true;

        /// <summary>
        /// �ֻ���
        /// </summary>
        public string Phone { get; set; } = "";

        /// <summary>
        /// ����
        /// </summary>
        public string Department { get; set; }
    }

}
